using Microsoft.Extensions.Logging;

namespace ISynergy.Tools.Azure.SignTool.Core.Abstractions.Services;

/// <summary>
/// Provides code signing functionality for authenticode signatures.
/// </summary>
public interface ICodeSigningService : IDisposable
{
    /// <summary>
    /// Authenticode signs a file.
    /// </summary>
    /// <param name="path">The path to the file to signed.</param>
    /// <param name="description">The description to apply to the signature.</param>
    /// <param name="descriptionUrl">A URL describing the signature or the signer.</param>
    /// <param name="pageHashing">True if the signing process should try to include page hashing, otherwise false.
    /// Use <c>null</c> to use the operating system default. Note that page hashing still may be disabled if the
    /// Subject Interface Package does not support page hashing.</param>
    /// <param name="logger">An optional logger to capture signing operations.</param>
    /// <param name="appendSignature"><see langword="true"/> if the signature should be appended to an existing signature. When <see langword="false"/>, any existing signatures will be replaced.</param>
    /// <returns>A HRESULT indicating the result of the signing operation. S_OK, or zero, is returned if the signing
    /// operation completed successfully.</returns>
    /// <exception cref="PlatformNotSupportedException"><paramref name="appendSignature"/> was set to <see langword="true"/> however the current operating system does not support appending signatures.</exception>
    int SignFile(ReadOnlySpan<char> path, ReadOnlySpan<char> description, ReadOnlySpan<char> descriptionUrl, bool? pageHashing, ILogger? logger = null, bool appendSignature = false);
}
