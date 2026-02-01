using System.Security.Cryptography;

namespace ISynergy.Tools.Azure.SignTool.Core.Abstractions.Services;

/// <summary>
/// Service interface for signing NuGet packages with a NuGet repository/author signature.
/// This creates the .signature.p7s file inside the package.
/// </summary>
public interface INuGetPackageSignatureService : IDisposable
{
    /// <summary>
    /// Signs a NuGet package with a NuGet package signature.
    /// </summary>
    /// <param name="packagePath">Path to the .nupkg file to sign.</param>
    /// <param name="outputPath">Path where the signed package will be written. Can be the same as packagePath to replace.</param>
    /// <param name="timestampUrl">Optional RFC 3161 timestamp server URL.</param>
    /// <param name="hashAlgorithm">The hash algorithm to use for signing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>0 for success, non-zero HRESULT for failure.</returns>
    Task<int> SignPackageAsync(
        string packagePath,
        string outputPath,
        string? timestampUrl,
        HashAlgorithmName hashAlgorithm,
        CancellationToken cancellationToken = default);
}
