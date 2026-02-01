using System.Security.Cryptography.X509Certificates;

namespace ISynergy.Tools.Azure.SignTool.Core.Abstractions.Stores;

/// <summary>
/// Represents a certificate store for code signing operations.
/// </summary>
public interface ICertificateStore : IDisposable
{
    /// <summary>
    /// Gets the handle to the certificate store.
    /// </summary>
    IntPtr Handle { get; }

    /// <summary>
    /// Gets the collection of certificates in the store.
    /// </summary>
    X509Certificate2Collection Certificates { get; }

    /// <summary>
    /// Adds a certificate to the store.
    /// </summary>
    /// <param name="certificate">The certificate to add.</param>
    void Add(X509Certificate2 certificate);

    /// <summary>
    /// Adds a collection of certificates to the store.
    /// </summary>
    /// <param name="collection">The collection of certificates to add.</param>
    void Add(X509Certificate2Collection collection);

    /// <summary>
    /// Closes the certificate store.
    /// </summary>
    void Close();
}
