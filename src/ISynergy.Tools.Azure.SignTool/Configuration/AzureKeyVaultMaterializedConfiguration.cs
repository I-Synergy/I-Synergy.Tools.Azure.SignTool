using Azure.Core;
using System.Security.Cryptography.X509Certificates;

namespace ISynergy.Tools.Azure.SignTool.Configuration;

public sealed record AzureKeyVaultMaterializedConfiguration(TokenCredential TokenCredential, X509Certificate2 PublicCertificate, Uri KeyId);

