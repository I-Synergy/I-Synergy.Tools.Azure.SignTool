using ISynergy.Tools.Azure.SignTool.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace ISynergy.Tools.Azure.SignTool.Core.Services;

/// <summary>
/// Service for signing NuGet packages (.nupkg and .snupkg) by signing the binaries contained within
/// </summary>
public class NuGetPackageSigner
{
    private readonly ICodeSigningService _codeSigningService;
    private readonly Microsoft.Extensions.Logging.ILogger? _logger;

    // File extensions that should be signed within NuGet packages
    private static readonly HashSet<string> SignableExtensions =
    [
        ".dll",
        ".exe",
        ".winmd"
    ];

    public NuGetPackageSigner(ICodeSigningService codeSigningService, Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        _codeSigningService = codeSigningService ?? throw new ArgumentNullException(nameof(codeSigningService));
        _logger = logger;
    }

    /// <summary>
    /// Signs a NuGet package by extracting it, signing all signable binaries, and repackaging
    /// </summary>
    /// <param name="packagePath">Path to the .nupkg or .snupkg file</param>
    /// <param name="description">Description for the signature</param>
    /// <param name="descriptionUrl">URL for more information about the signature</param>
    /// <param name="pageHashing">Whether to perform page hashing</param>
    /// <param name="appendSignature">Whether to append the signature</param>
    /// <returns>HRESULT indicating success or failure</returns>
    public int SignPackage(
        string packagePath,
        ReadOnlySpan<char> description,
        ReadOnlySpan<char> descriptionUrl,
        bool? pageHashing,
        bool appendSignature = false)
    {
        _logger?.LogInformation("Signing NuGet package: {Package}", packagePath);

        if (!File.Exists(packagePath))
        {
            _logger?.LogError("Package file does not exist: {Package}", packagePath);
            return unchecked((int)0x80070002); // ERROR_FILE_NOT_FOUND
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"nuget_sign_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);
            _logger?.LogTrace("Created temporary directory: {TempDir}", tempDir);

            // Extract the package
            _logger?.LogTrace("Extracting package contents");
            ZipFile.ExtractToDirectory(packagePath, tempDir);

            // Find and sign all signable files
            var signableFiles = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories)
                .Where(IsSignableFile)
                .ToList();

            if (signableFiles.Count == 0)
            {
                _logger?.LogInformation("No signable files found in package");
            }
            else
            {
                _logger?.LogInformation("Found {Count} signable file(s) in package", signableFiles.Count);

                foreach (var file in signableFiles)
                {
                    _logger?.LogTrace("Signing file: {File}", Path.GetFileName(file));
                    var result = _codeSigningService.SignFile(file, description, descriptionUrl, pageHashing, _logger, appendSignature);

                    if (result != 0) // S_OK = 0
                    {
                        _logger?.LogError("Failed to sign file {File} with result {Result:X8}", Path.GetFileName(file), result);
                        return result;
                    }
                }
            }

            // Repackage the files
            _logger?.LogTrace("Repackaging signed contents");
            var tempPackagePath = $"{packagePath}.tmp";

            // Delete temp package if it exists
            if (File.Exists(tempPackagePath))
            {
                File.Delete(tempPackagePath);
            }

            // Create new package with signed files, using the original compression level
            using (var originalArchive = ZipFile.OpenRead(packagePath))
            {
                using var newArchive = ZipFile.Open(tempPackagePath, ZipArchiveMode.Create);
                
                // Copy all entries from temp directory, preserving structure
                foreach (var filePath in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
                {
                    var entryName = Path.GetRelativePath(tempDir, filePath).Replace('\\', '/');
                    newArchive.CreateEntryFromFile(filePath, entryName, CompressionLevel.Optimal);
                }
            }

            // Replace original package with signed package
            File.Delete(packagePath);
            File.Move(tempPackagePath, packagePath);

            _logger?.LogInformation("Successfully signed NuGet package");
            return 0; // S_OK
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error signing NuGet package: {Message}", ex.Message);
            return unchecked((int)0x80004005); // E_FAIL
        }
        finally
        {
            // Cleanup temporary directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                    _logger?.LogTrace("Cleaned up temporary directory");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to clean up temporary directory: {TempDir}", tempDir);
            }
        }
    }

    private static bool IsSignableFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return SignableExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a file is a NuGet package
    /// </summary>
    public static bool IsNuGetPackage(ReadOnlySpan<char> filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.Equals(".nupkg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".snupkg", StringComparison.OrdinalIgnoreCase);
    }
}


