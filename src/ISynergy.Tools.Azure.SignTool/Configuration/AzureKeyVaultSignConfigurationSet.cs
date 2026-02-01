using ISynergy.Tools.Azure.SignTool.Enumerations;

namespace ISynergy.Tools.Azure.SignTool.Configuration;

public sealed class AzureKeyVaultSignConfigurationSet
{
    public bool ManagedIdentity { get; init; }
    public AzureCredentialType? AzureCredentialType { get; init; }
    public string? AzureClientId { get; init; }
    public string? AzureClientSecret { get; init; }
    public string? AzureTenantId { get; init; }
    public Uri? AzureKeyVaultUrl { get; init; }
    public string? AzureKeyVaultCertificateName { get; init; }
    public string? AzureKeyVaultCertificateVersion { get; init; }
    public string? AzureAccessToken { get; init; }
    public string? AzureAuthority { get; init; }
}
