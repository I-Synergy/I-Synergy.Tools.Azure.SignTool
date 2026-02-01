using ISynergy.Tools.Azure.SignTool.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Packaging.Signing;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace ISynergy.Tools.Azure.SignTool.Core.Services;

/// <summary>
/// Service for creating NuGet package signatures using Azure Key Vault.
/// This creates the .signature.p7s file that makes the package show as "Signature: Valid" in NuGet Package Explorer.
/// </summary>
public class NuGetPackageSignatureService : INuGetPackageSignatureService
{
    private readonly RSA _signingAlgorithm;
    private readonly X509Certificate2 _certificate;
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

        // Store the Key Vault RSA and certificate separately.
        // We do NOT use CopyWithPrivateKey because Azure Key Vault keys are non-exportable.
        // Instead, we create a custom signature provider that uses the RSA directly for signing operations.
        _signingAlgorithm = signingAlgorithm;
        _certificate = certificate;
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

            // Create the custom Key Vault signature provider
            var signatureProvider = new KeyVaultSignatureProvider(_signingAlgorithm, timestampProvider);

            // Determine the hash algorithm for NuGet
            var nugetHashAlgorithm = hashAlgorithm.Name switch
            {
                "SHA256" => NuGet.Common.HashAlgorithmName.SHA256,
                "SHA384" => NuGet.Common.HashAlgorithmName.SHA384,
                "SHA512" => NuGet.Common.HashAlgorithmName.SHA512,
                _ => NuGet.Common.HashAlgorithmName.SHA256
            };

            // Create an author signature request using the certificate (without requiring private key access)
            var request = new AuthorSignPackageRequest(
                _certificate,
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
        // Certificate is owned by the caller, don't dispose it here
    }

    /// <summary>
    /// Custom signature provider that uses Azure Key Vault RSA for signing operations.
    /// This avoids the need to export private key material from Key Vault.
    /// </summary>
    private sealed class KeyVaultSignatureProvider : ISignatureProvider
    {
        private readonly RSA _rsa;
        private readonly ITimestampProvider? _timestampProvider;

        public KeyVaultSignatureProvider(RSA rsa, ITimestampProvider? timestampProvider = null)
        {
            _rsa = rsa ?? throw new ArgumentNullException(nameof(rsa));
            _timestampProvider = timestampProvider;
        }

        // OIDs for NuGet signature attributes
        private static readonly Oid CommitmentTypeIndicationOid = new("1.2.840.113549.1.9.16.2.16");
        private static readonly Oid ProofOfOriginOid = new("1.2.840.113549.1.9.16.6.1");
        private static readonly Oid SigningCertificateV2Oid = new("1.2.840.113549.1.9.16.2.47");

        public async Task<PrimarySignature> CreatePrimarySignatureAsync(
            SignPackageRequest request,
            SignatureContent signatureContent,
            NuGet.Common.ILogger logger,
            CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(signatureContent);
            ArgumentNullException.ThrowIfNull(logger);

            // Get the hash algorithm
            var hashAlgorithm = GetHashAlgorithmName(request.SignatureHashAlgorithm);

            // Create the CMS signer using the certificate and Key Vault RSA
            // This constructor allows passing the RSA separately, so the certificate
            // doesn't need to have HasPrivateKey = true
            var cmsSigner = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, request.Certificate, _rsa)
            {
                DigestAlgorithm = new Oid(hashAlgorithm.Name ?? "SHA256"),
                IncludeOption = X509IncludeOption.WholeChain
            };

            // Add signing time attribute
            cmsSigner.SignedAttributes.Add(new Pkcs9SigningTime());

            // Add commitment type indication attribute (required for NuGet author signatures)
            // This identifies the signature as an "author" signature (proof of origin)
            cmsSigner.SignedAttributes.Add(CreateCommitmentTypeIndicationAttribute());

            // Add signing certificate V2 attribute (ESSCertIDv2)
            // This links the signature to the specific signing certificate
            cmsSigner.SignedAttributes.Add(CreateSigningCertificateV2Attribute(request.Certificate, hashAlgorithm));

            // Create the content info from the signature content
            var contentInfo = new ContentInfo(signatureContent.GetBytes());

            // Create and compute the CMS signature
            var signedCms = new SignedCms(contentInfo);
            signedCms.ComputeSignature(cmsSigner, silent: true);

            // Get the encoded signature
            var signature = signedCms.Encode();

            // Create the primary signature
            var primarySignature = PrimarySignature.Load(signature);

            // Add timestamp if provider is available
            if (_timestampProvider != null)
            {
                primarySignature = await TimestampPrimarySignatureAsync(
                    request,
                    primarySignature,
                    logger,
                    token);
            }

            return primarySignature;
        }

        /// <summary>
        /// Creates the commitment type indication attribute for author signatures.
        /// This is required by NuGet to identify the signature type.
        /// </summary>
        private static AsnEncodedData CreateCommitmentTypeIndicationAttribute()
        {
            // CommitmentTypeIndication ::= SEQUENCE {
            //   commitmentTypeId CommitmentTypeIdentifier,
            //   commitmentTypeQualifier SEQUENCE SIZE (1..MAX) OF CommitmentTypeQualifier OPTIONAL
            // }
            // For author signatures, we use proofOfOrigin (1.2.840.113549.1.9.16.6.1)
            var writer = new AsnWriter(AsnEncodingRules.DER);
            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(ProofOfOriginOid.Value!);
            }

            return new AsnEncodedData(CommitmentTypeIndicationOid, writer.Encode());
        }

        /// <summary>
        /// Creates the signing certificate V2 attribute (ESSCertIDv2).
        /// This links the signature to the specific signing certificate.
        /// </summary>
        private static AsnEncodedData CreateSigningCertificateV2Attribute(
            X509Certificate2 certificate,
            System.Security.Cryptography.HashAlgorithmName hashAlgorithm)
        {
            // SigningCertificateV2 ::= SEQUENCE {
            //   certs SEQUENCE OF ESSCertIDv2,
            //   policies SEQUENCE OF PolicyInformation OPTIONAL
            // }
            // ESSCertIDv2 ::= SEQUENCE {
            //   hashAlgorithm AlgorithmIdentifier DEFAULT {algorithm id-sha256},
            //   certHash Hash,
            //   issuerSerial IssuerSerial OPTIONAL
            // }
            // IssuerSerial ::= SEQUENCE {
            //   issuer GeneralNames,
            //   serialNumber CertificateSerialNumber
            // }

            // Compute the certificate hash
            byte[] certHash;
            string algorithmOid;

            using (var hasher = IncrementalHash.CreateHash(hashAlgorithm))
            {
                hasher.AppendData(certificate.RawData);
                certHash = hasher.GetHashAndReset();
            }

            // Get the algorithm OID
            algorithmOid = hashAlgorithm.Name switch
            {
                "SHA256" => "2.16.840.1.101.3.4.2.1",
                "SHA384" => "2.16.840.1.101.3.4.2.2",
                "SHA512" => "2.16.840.1.101.3.4.2.3",
                _ => "2.16.840.1.101.3.4.2.1" // Default to SHA256
            };

            var writer = new AsnWriter(AsnEncodingRules.DER);

            // SigningCertificateV2 SEQUENCE
            using (writer.PushSequence())
            {
                // certs SEQUENCE OF ESSCertIDv2
                using (writer.PushSequence())
                {
                    // ESSCertIDv2 SEQUENCE
                    using (writer.PushSequence())
                    {
                        // hashAlgorithm AlgorithmIdentifier (only include if not SHA256, as SHA256 is default)
                        if (hashAlgorithm != System.Security.Cryptography.HashAlgorithmName.SHA256)
                        {
                            using (writer.PushSequence())
                            {
                                writer.WriteObjectIdentifier(algorithmOid);
                                writer.WriteNull();
                            }
                        }

                        // certHash OCTET STRING
                        writer.WriteOctetString(certHash);

                        // issuerSerial SEQUENCE
                        using (writer.PushSequence())
                        {
                            // issuer GeneralNames (SEQUENCE OF GeneralName)
                            using (writer.PushSequence())
                            {
                                // GeneralName - directoryName [4]
                                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 4)))
                                {
                                    // Write the issuer distinguished name
                                    writer.WriteEncodedValue(certificate.IssuerName.RawData);
                                }
                            }

                            // serialNumber INTEGER
                            var serialBytes = certificate.GetSerialNumber();
                            // .NET returns serial number in little-endian, ASN.1 needs big-endian
                            Array.Reverse(serialBytes);
                            writer.WriteIntegerUnsigned(serialBytes);
                        }
                    }
                }
            }

            return new AsnEncodedData(SigningCertificateV2Oid, writer.Encode());
        }

        public Task<PrimarySignature> CreateRepositoryCountersignatureAsync(
            RepositorySignPackageRequest request,
            PrimarySignature primarySignature,
            NuGet.Common.ILogger logger,
            CancellationToken token)
        {
            // Repository countersignatures are not commonly used and would require
            // additional implementation. For now, return the primary signature unchanged.
            throw new NotSupportedException("Repository countersignatures are not supported with Key Vault signing.");
        }

        private async Task<PrimarySignature> TimestampPrimarySignatureAsync(
            SignPackageRequest request,
            PrimarySignature signature,
            NuGet.Common.ILogger logger,
            CancellationToken token)
        {
            if (_timestampProvider == null)
            {
                return signature;
            }

            var signatureValue = signature.GetSignatureValue();
            var messageHash = GetHashAlgorithmName(request.TimestampHashAlgorithm);

            using var hashAlgorithm = IncrementalHash.CreateHash(messageHash);
            hashAlgorithm.AppendData(signatureValue);
            var hash = hashAlgorithm.GetHashAndReset();

            var timestampRequest = new TimestampRequest(
                signingSpecifications: SigningSpecifications.V1,
                hashedMessage: hash,
                hashAlgorithm: request.TimestampHashAlgorithm,
                target: SignaturePlacement.PrimarySignature);

            var timestampedSignature = await _timestampProvider.TimestampSignatureAsync(
                signature,
                timestampRequest,
                logger,
                token);

            return timestampedSignature;
        }

        private static System.Security.Cryptography.HashAlgorithmName GetHashAlgorithmName(NuGet.Common.HashAlgorithmName hashAlgorithm)
        {
            return hashAlgorithm switch
            {
                NuGet.Common.HashAlgorithmName.SHA256 => System.Security.Cryptography.HashAlgorithmName.SHA256,
                NuGet.Common.HashAlgorithmName.SHA384 => System.Security.Cryptography.HashAlgorithmName.SHA384,
                NuGet.Common.HashAlgorithmName.SHA512 => System.Security.Cryptography.HashAlgorithmName.SHA512,
                _ => System.Security.Cryptography.HashAlgorithmName.SHA256
            };
        }
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
