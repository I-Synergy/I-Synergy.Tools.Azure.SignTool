namespace ISynergy.Tools.Azure.SignTool.Enumerations;

/// <summary>
/// Specifies the type of Azure credential to use for authentication.
/// </summary>
public enum AzureCredentialType
{
    /// <summary>
    /// Use DefaultAzureCredential which tries multiple credential types in sequence.
    /// Not recommended for production scenarios.
    /// </summary>
    DefaultAzureCredential,

    /// <summary>
    /// Use ManagedIdentityCredential for Azure managed identity authentication.
    /// </summary>
    ManagedIdentityCredential,

    /// <summary>
    /// Use WorkloadIdentityCredential for workload identity federation.
    /// </summary>
    WorkloadIdentityCredential,

    /// <summary>
    /// Use InteractiveBrowserCredential for interactive browser authentication.
    /// </summary>
    InteractiveBrowserCredential,

    /// <summary>
    /// Use EnvironmentCredential to authenticate using environment variables.
    /// </summary>
    EnvironmentCredential,

    /// <summary>
    /// Use AzureCliCredential to authenticate using Azure CLI.
    /// </summary>
    AzureCliCredential,

    /// <summary>
    /// Use AzurePowerShellCredential to authenticate using Azure PowerShell.
    /// </summary>
    AzurePowerShellCredential,

    /// <summary>
    /// Use ClientSecretCredential to authenticate using client secret.
    /// This is the default when client ID and secret are provided.
    /// </summary>
    ClientSecretCredential,

    /// <summary>
    /// Use AccessTokenCredential to authenticate using an access token.
    /// This is the default when an access token is provided.
    /// </summary>
    AccessTokenCredential
}
