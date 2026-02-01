using ISynergy.Tools.Azure.SignTool.Core.Abstractions.Services;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Security.Cryptography;

namespace ISynergy.Tools.Azure.SignTool.Core.Services;

/// <summary>
/// Service for signing NuGet packages (.nupkg and .snupkg) by signing the binaries contained within
/// and optionally adding a NuGet package signature.
/// </summary>
public class NuGetPackageSigner
{
    private readonly ICodeSigningService _codeSigningService;
    private readonly INuGetPackageSignatureService? _nugetSignatureService;
    private readonly Microsoft.Extensions.Logging.ILogger? _logger;
    private readonly string? _timestampUrl;
    private readonly HashAlgorithmName _hashAlgorithm;

    // File extensions that should be signed within NuGet packages
    private static readonly HashSet<string> SignableExtensions =
    [
        ".dll",
        ".exe",
        ".winmd"
    ];

    /// <summary>
    /// Creates a new instance of <see cref="NuGetPackageSigner"/> without NuGet package signing.
    /// Only signs the binaries inside the package.
    /// </summary>
    public NuGetPackageSigner(ICodeSigningService codeSigningService, Microsoft.Extensions.Logging.ILogger? logger = null)
        : this(codeSigningService, null, null, HashAlgorithmName.SHA256, logger)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="NuGetPackageSigner"/> with full NuGet package signing support.
    /// Signs binaries inside the package AND creates a NuGet package signature.
    /// </summary>
    /// <param name="codeSigningService">Service for signing binaries inside the package.</param>
    /// <param name="nugetSignatureService">Service for creating NuGet package signatures. If null, only binaries are signed.</param>
    /// <param name="timestampUrl">RFC 3161 timestamp server URL for NuGet package signature.</param>
    /// <param name="hashAlgorithm">Hash algorithm for NuGet package signature.</param>
    /// <param name="logger">Optional logger.</param>
    public NuGetPackageSigner(
        ICodeSigningService codeSigningService,
        INuGetPackageSignatureService? nugetSignatureService,
        string? timestampUrl,
        HashAlgorithmName hashAlgorithm,
        Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        _codeSigningService = codeSigningService ?? throw new ArgumentNullException(nameof(codeSigningService));
        _nugetSignatureService = nugetSignatureService;
        _timestampUrl = timestampUrl;
        _hashAlgorithm = hashAlgorithm;
        _logger = logger;
    }

    /// <summary>
    /// Signs a NuGet package by extracting it, signing all signable binaries, repackaging,
    /// and optionally adding a NuGet package signature.
    /// </summary>
    /// <param name="packagePath">Path to the .nupkg or .snupkg file</param>
    /// <param name="description">Description for the signature</param>
    /// <param name="descriptionUrl">URL for more information about the signature</param>
    /// <param name="pageHashing">Whether to perform page hashing</param>
    /// <param name="appendSignature">Whether to append the signature</param>
    /// <param name="signContents">Whether to sign the binaries inside the package. Default is false (only signs the package itself).</param>
    /// <param name="contentsFilter">Optional glob pattern to filter which files to sign (e.g., "ISynergy.Framework.*"). If null, all signable files are signed.</param>
    /// <returns>HRESULT indicating success or failure</returns>
    public int SignPackage(
        string packagePath,
        ReadOnlySpan<char> description,
        ReadOnlySpan<char> descriptionUrl,
        bool? pageHashing,
        bool appendSignature = false,
        bool signContents = false,
        string? contentsFilter = null)
    {
        // Call the async version synchronously for backwards compatibility
        return SignPackageAsync(packagePath, description.ToString(), descriptionUrl.ToString(), pageHashing, appendSignature, signContents, contentsFilter)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Signs a NuGet package asynchronously by extracting it, signing all signable binaries, repackaging,
    /// and optionally adding a NuGet package signature.
    /// </summary>
    /// <param name="packagePath">Path to the .nupkg or .snupkg file</param>
    /// <param name="description">Description for the signature</param>
    /// <param name="descriptionUrl">URL for more information about the signature</param>
    /// <param name="pageHashing">Whether to perform page hashing</param>
    /// <param name="appendSignature">Whether to append the signature</param>
    /// <param name="signContents">Whether to sign the binaries inside the package. Default is false (only signs the package itself).</param>
    /// <param name="contentsFilter">Optional glob pattern to filter which files to sign (e.g., "ISynergy.Framework.*"). If null, all signable files are signed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HRESULT indicating success or failure</returns>
    public async Task<int> SignPackageAsync(
        string packagePath,
        string? description,
        string? descriptionUrl,
        bool? pageHashing,
        bool appendSignature = false,
        bool signContents = false,
        string? contentsFilter = null,
        CancellationToken cancellationToken = default)
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
            // Only extract and sign contents if signContents is true
            if (signContents)
            {
                Directory.CreateDirectory(tempDir);
                _logger?.LogTrace("Created temporary directory: {TempDir}", tempDir);

                // Extract the package
                _logger?.LogTrace("Extracting package contents");
                ZipFile.ExtractToDirectory(packagePath, tempDir);

                // Find all signable files
                var allSignableFiles = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories)
                    .Where(IsSignableFile)
                    .ToList();

                // Apply filter if specified
                List<string> filesToSign;
                if (!string.IsNullOrWhiteSpace(contentsFilter))
                {
                    _logger?.LogInformation("Applying content filter: {Filter}", contentsFilter);
                    filesToSign = allSignableFiles
                        .Where(f => MatchesFilter(Path.GetFileName(f), contentsFilter))
                        .ToList();

                    var skippedCount = allSignableFiles.Count - filesToSign.Count;
                    if (skippedCount > 0)
                    {
                        _logger?.LogInformation("Skipping {Count} file(s) not matching filter", skippedCount);
                    }
                }
                else
                {
                    filesToSign = allSignableFiles;
                }

                if (filesToSign.Count == 0)
                {
                    _logger?.LogInformation("No signable files found in package{FilterNote}",
                        !string.IsNullOrWhiteSpace(contentsFilter) ? " matching filter" : "");
                }
                else
                {
                    _logger?.LogInformation("Signing {Count} file(s) in package", filesToSign.Count);

                    foreach (var file in filesToSign)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        _logger?.LogTrace("Signing file: {File}", Path.GetFileName(file));
                        var result = _codeSigningService.SignFile(
                            file,
                            description ?? string.Empty,
                            descriptionUrl ?? string.Empty,
                            pageHashing,
                            _logger,
                            appendSignature);

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

                _logger?.LogInformation("Successfully signed binaries in NuGet package");
            }
            else
            {
                _logger?.LogTrace("Skipping content signing (--sign-nuget-contents not specified)");
            }

            // Now sign the package itself if NuGet signature service is available
            if (_nugetSignatureService is not null)
            {
                // Only sign .nupkg files with NuGet signature, not .snupkg (symbol packages don't get NuGet signatures)
                if (packagePath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogInformation("Creating NuGet package signature");
                    var signResult = await _nugetSignatureService.SignPackageAsync(
                        packagePath,
                        packagePath,
                        _timestampUrl,
                        _hashAlgorithm,
                        cancellationToken);

                    if (signResult != 0)
                    {
                        _logger?.LogError("Failed to create NuGet package signature with result {Result:X8}", signResult);
                        return signResult;
                    }

                    _logger?.LogInformation("NuGet package signature created successfully");
                }
                else
                {
                    _logger?.LogTrace("Skipping NuGet package signature for symbol package (.snupkg)");
                }
            }
            else
            {
                _logger?.LogTrace("NuGet package signature service not configured, skipping package signature");
            }

            return 0; // S_OK
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Signing operation was cancelled");
            throw;
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
    /// Checks if a file name matches the specified glob pattern.
    /// </summary>
    /// <param name="fileName">The file name to check.</param>
    /// <param name="pattern">The glob pattern (e.g., "ISynergy.Framework.*" or "MyCompany.*.dll").</param>
    /// <returns>True if the file name matches the pattern.</returns>
    private static bool MatchesFilter(string fileName, string pattern)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(pattern);

        // Create a virtual directory structure with just the file name
        var result = matcher.Match(fileName);
        return result.HasMatches;
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
