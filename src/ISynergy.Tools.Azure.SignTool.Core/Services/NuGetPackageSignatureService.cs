using ISynergy.Tools.Azure.SignTool.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Packaging.Signing;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ISynergy.Tools.Azure.SignTool.Core.Services;

/// <summary>
/// Service for creating NuGet package signatures using Azure Key Vault.
/// This creates the .signature.p7s file that makes the package show as "Signature: Valid" in NuGet Package Explorer.
/// </summary>
public class NuGetPackageSignatureService : INuGetPackageSignatureService
{
    private readonly X509Certificate2 _certificateWithKey;
    private readonly Microsoft.Extensions.Logging.ILogger? _logger;

    /// <summary>
    /// Creates a new instance of <see cref="NuGetPackageSignatureService"/>.
    /// </summary>
    /// <param name="signingAlgorithm">The RSA algorithm instance (backed by Azure Key Vault).</param>
    /// <param name="certificate">The public certificate from Azure Key Vault.</param>
    /// <param name="logger">Optional logger.</param>
    public NuGetPackageSignatureService(
        RSA signingAlgorithm,
        X509Certificate2 certificate,
        Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(signingAlgorithm);
        ArgumentNullException.ThrowIfNull(certificate);

        // Create a certificate that appears to have a private key
        // The actual signing will be done through the Azure Key Vault RSA instance
        _certificateWithKey = certificate.CopyWithPrivateKey(signingAlgorithm);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<int> SignPackageAsync(
        string packagePath,
        string outputPath,
        string? timestampUrl,
        System.Security.Cryptography.HashAlgorithmName hashAlgorithm,
        CancellationToken cancellationToken = default)
    {
        const int S_OK = 0;
        const int E_FAIL = unchecked((int)0x80004005);
        const int E_FILENOTFOUND = unchecked((int)0x80070002);

        try
        {
            if (!File.Exists(packagePath))
            {
                _logger?.LogError("Package file does not exist: {Package}", packagePath);
                return E_FILENOTFOUND;
            }

            _logger?.LogInformation("Creating NuGet package signature for: {Package}", packagePath);

            // Create timestamp provider if URL is provided
            ITimestampProvider? timestampProvider = null;
            if (!string.IsNullOrEmpty(timestampUrl))
            {
                _logger?.LogTrace("Using timestamp server: {Url}", timestampUrl);
                timestampProvider = new Rfc3161TimestampProvider(new Uri(timestampUrl));
            }

            // Create the signature provider
            var signatureProvider = new X509SignatureProvider(timestampProvider);

            // Determine the hash algorithm for NuGet
            var nugetHashAlgorithm = hashAlgorithm.Name switch
            {
                "SHA256" => NuGet.Common.HashAlgorithmName.SHA256,
                "SHA384" => NuGet.Common.HashAlgorithmName.SHA384,
                "SHA512" => NuGet.Common.HashAlgorithmName.SHA512,
                _ => NuGet.Common.HashAlgorithmName.SHA256
            };

            // Create an author signature request
            var request = new AuthorSignPackageRequest(
                _certificateWithKey,
                nugetHashAlgorithm,
                nugetHashAlgorithm);

            // Create a NuGet logger adapter
            var nugetLogger = new NuGetLoggerAdapter(_logger);

            // Determine if we need to use a temp file
            bool sameFile = string.Equals(packagePath, outputPath, StringComparison.OrdinalIgnoreCase);
            string actualOutputPath = sameFile ? $"{packagePath}.signed.tmp" : outputPath;

            // Sign the package
            using (var packageReadStream = File.OpenRead(packagePath))
            using (var packageWriteStream = File.Create(actualOutputPath))
            {
                await SigningUtility.SignAsync(
                    new SigningOptions(
                        new Lazy<Stream>(() => packageReadStream),
                        new Lazy<Stream>(() => packageWriteStream),
                        overwrite: false,
                        signatureProvider,
                        nugetLogger),
                    request,
                    cancellationToken);
            }

            // If signing the same file, replace the original
            if (sameFile)
            {
                File.Delete(packagePath);
                File.Move(actualOutputPath, packagePath);
            }

            _logger?.LogInformation("NuGet package signature created successfully");

            return S_OK;
        }
        catch (SignatureException ex)
        {
            _logger?.LogError(ex, "NuGet signing failed: {Message}", ex.Message);
            return E_FAIL;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating NuGet package signature: {Message}", ex.Message);
            return E_FAIL;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _certificateWithKey?.Dispose();
    }

    /// <summary>
    /// Adapter to bridge Microsoft.Extensions.Logging to NuGet.Common.ILogger.
    /// </summary>
    private class NuGetLoggerAdapter : NuGet.Common.ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger? _logger;

        public NuGetLoggerAdapter(Microsoft.Extensions.Logging.ILogger? logger)
        {
            _logger = logger;
        }

        public void Log(NuGet.Common.LogLevel level, string data)
        {
            var msLevel = level switch
            {
                NuGet.Common.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
                NuGet.Common.LogLevel.Verbose => Microsoft.Extensions.Logging.LogLevel.Trace,
                NuGet.Common.LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
                NuGet.Common.LogLevel.Minimal => Microsoft.Extensions.Logging.LogLevel.Information,
                NuGet.Common.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
                NuGet.Common.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
                _ => Microsoft.Extensions.Logging.LogLevel.Information
            };

            _logger?.Log(msLevel, "{Message}", data);
        }

        public void Log(ILogMessage message) => Log(message.Level, message.Message);

        public Task LogAsync(NuGet.Common.LogLevel level, string data)
        {
            Log(level, data);
            return Task.CompletedTask;
        }

        public Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }

        public void LogDebug(string data) => Log(NuGet.Common.LogLevel.Debug, data);
        public void LogError(string data) => Log(NuGet.Common.LogLevel.Error, data);
        public void LogInformation(string data) => Log(NuGet.Common.LogLevel.Information, data);
        public void LogInformationSummary(string data) => Log(NuGet.Common.LogLevel.Information, data);
        public void LogMinimal(string data) => Log(NuGet.Common.LogLevel.Minimal, data);
        public void LogVerbose(string data) => Log(NuGet.Common.LogLevel.Verbose, data);
        public void LogWarning(string data) => Log(NuGet.Common.LogLevel.Warning, data);
    }
}
