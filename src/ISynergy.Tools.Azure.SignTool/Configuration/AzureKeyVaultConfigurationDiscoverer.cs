using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using ISynergy.Tools.Azure.SignTool.Credentials;
using ISynergy.Tools.Azure.SignTool.Enumerations;
using ISynergy.Tools.Azure.SignTool.Results;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace ISynergy.Tools.Azure.SignTool.Configuration;

internal class AzureKeyVaultConfigurationDiscoverer
{
    private readonly ILogger _logger;

    public AzureKeyVaultConfigurationDiscoverer(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<AzureKeyVaultMaterializedConfiguration>> Materialize(AzureKeyVaultSignConfigurationSet configuration)
    {
        TokenCredential credential;
        
        // If a specific credential type is specified, use it
        if (configuration.AzureCredentialType.HasValue)
        {
            _logger.LogInformation($"Using explicitly specified credential type: {configuration.AzureCredentialType.Value}");
            credential = CreateCredentialByType(configuration);
        }
        // Legacy behavior: use ManagedIdentity flag for backward compatibility
        else if (configuration.ManagedIdentity)
        {
            _logger.LogInformation("Using DefaultAzureCredential (legacy --azure-key-vault-managed-identity flag)");
            _logger.LogWarning("DefaultAzureCredential is not recommended for production. Consider using --azure-credential-type to explicitly specify the credential type.");
            credential = new DefaultAzureCredential();
        }
        else if (!string.IsNullOrWhiteSpace(configuration.AzureAccessToken))
        {
            _logger.LogInformation("Using AccessTokenCredential (access token provided)");
            credential = new AccessTokenCredential(configuration.AzureAccessToken);
        }
        else
        {
            _logger.LogInformation("Using ClientSecretCredential (client ID and secret provided)");
            if (string.IsNullOrWhiteSpace(configuration.AzureAuthority))
            {
                credential = new ClientSecretCredential(configuration.AzureTenantId, configuration.AzureClientId, configuration.AzureClientSecret);
            }
            else
            {
                _logger.LogInformation($"Using Azure Authority: {configuration.AzureAuthority}");
                ClientSecretCredentialOptions options = new()
                {
                    AuthorityHost = AuthorityHostNames.GetUriForAzureAuthorityIdentifier(configuration.AzureAuthority)
                };
                credential = new ClientSecretCredential(configuration.AzureTenantId, configuration.AzureClientId, configuration.AzureClientSecret, options);
            }
        }


        X509Certificate2 certificate;
        KeyVaultCertificate azureCertificate;
        try
        {
            var certClient = new CertificateClient(configuration.AzureKeyVaultUrl, credential);

            if (!string.IsNullOrWhiteSpace(configuration.AzureKeyVaultCertificateVersion))
            {
                _logger.LogTrace($"Retrieving version [{configuration.AzureKeyVaultCertificateVersion}] of certificate {configuration.AzureKeyVaultCertificateName}.");
                azureCertificate = (await certClient.GetCertificateVersionAsync(configuration.AzureKeyVaultCertificateName, configuration.AzureKeyVaultCertificateVersion).ConfigureAwait(false)).Value;
            }
            else
            {
                _logger.LogTrace($"Retrieving current version of certificate {configuration.AzureKeyVaultCertificateName}.");
                azureCertificate = (await certClient.GetCertificateAsync(configuration.AzureKeyVaultCertificateName).ConfigureAwait(false)).Value;
            }
            _logger.LogTrace($"Retrieved certificate with Id {azureCertificate.Id}.");

            certificate = X509CertificateLoader.LoadCertificate(azureCertificate.Cer);
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to retrieve certificate {configuration.AzureKeyVaultCertificateName} from Azure Key Vault. Please verify the name of the certificate and the permissions to the certificate. Error message: {e.Message}.");
            _logger.LogTrace(e.ToString());

            return e;
        }
        var keyId = azureCertificate.KeyId;

        if (keyId is null)
        {
            return new InvalidOperationException("The Azure certificate does not have an associated private key.");
        }

        return new AzureKeyVaultMaterializedConfiguration(credential, certificate, keyId);
    }

    private TokenCredential CreateCredentialByType(AzureKeyVaultSignConfigurationSet configuration)
    {
        Uri? authorityHost = string.IsNullOrWhiteSpace(configuration.AzureAuthority) 
            ? null 
            : AuthorityHostNames.GetUriForAzureAuthorityIdentifier(configuration.AzureAuthority);

        if (authorityHost is not null)
        {
            _logger.LogInformation($"Using Azure Authority: {configuration.AzureAuthority}");
        }

        return configuration.AzureCredentialType switch
        {
            AzureCredentialType.DefaultAzureCredential => CreateDefaultAzureCredential(authorityHost),
            AzureCredentialType.ManagedIdentityCredential => CreateManagedIdentityCredential(configuration, authorityHost),
            AzureCredentialType.WorkloadIdentityCredential => CreateWorkloadIdentityCredential(configuration, authorityHost),
            AzureCredentialType.InteractiveBrowserCredential => CreateInteractiveBrowserCredential(configuration, authorityHost),
            AzureCredentialType.EnvironmentCredential => CreateEnvironmentCredential(authorityHost),
            AzureCredentialType.AzureCliCredential => CreateAzureCliCredential(),
            AzureCredentialType.AzurePowerShellCredential => CreateAzurePowerShellCredential(),
            AzureCredentialType.ClientSecretCredential => CreateClientSecretCredential(configuration, authorityHost),
            AzureCredentialType.AccessTokenCredential => CreateAccessTokenCredential(configuration),
            _ => throw new ArgumentException($"Unsupported credential type: {configuration.AzureCredentialType}")
        };
    }

    private TokenCredential CreateDefaultAzureCredential(Uri? authorityHost)
    {
        _logger.LogWarning("Using DefaultAzureCredential is not recommended for production scenarios. Consider using a specific credential type.");
        _logger.LogTrace("DefaultAzureCredential will attempt credentials in this order: EnvironmentCredential, WorkloadIdentityCredential, ManagedIdentityCredential, SharedTokenCacheCredential, VisualStudioCredential, VisualStudioCodeCredential, AzureCliCredential, AzurePowerShellCredential, AzureDeveloperCliCredential, InteractiveBrowserCredential");
        
        if (authorityHost is not null)
        {
            DefaultAzureCredentialOptions options = new()
            {
                AuthorityHost = authorityHost
            };
            return new DefaultAzureCredential(options);
        }
        return new DefaultAzureCredential();
    }

    private TokenCredential CreateManagedIdentityCredential(AzureKeyVaultSignConfigurationSet configuration, Uri? authorityHost)
    {
        if (!string.IsNullOrWhiteSpace(configuration.AzureClientId))
        {
            _logger.LogInformation($"Creating ManagedIdentityCredential with Client ID: {configuration.AzureClientId}");
        }
        else
        {
            _logger.LogInformation("Creating ManagedIdentityCredential with system-assigned identity");
        }

        ManagedIdentityCredentialOptions? options = null;
        
        if (authorityHost is not null)
        {
            options = new ManagedIdentityCredentialOptions
            {
                AuthorityHost = authorityHost
            };
        }
        
        if (!string.IsNullOrWhiteSpace(configuration.AzureClientId))
        {
            if (options is not null)
            {
                return new ManagedIdentityCredential(configuration.AzureClientId, options);
            }
            return new ManagedIdentityCredential(configuration.AzureClientId);
        }
        
        if (options is not null)
        {
            return new ManagedIdentityCredential(options);
        }
        
        return new ManagedIdentityCredential();
    }

    private TokenCredential CreateWorkloadIdentityCredential(AzureKeyVaultSignConfigurationSet configuration, Uri? authorityHost)
    {
        _logger.LogInformation("Creating WorkloadIdentityCredential for workload identity federation");
        
        WorkloadIdentityCredentialOptions options = new();
        
        if (authorityHost is not null)
        {
            options.AuthorityHost = authorityHost;
        }
        
        if (!string.IsNullOrWhiteSpace(configuration.AzureTenantId))
        {
            _logger.LogTrace($"Using Tenant ID: {configuration.AzureTenantId}");
            options.TenantId = configuration.AzureTenantId;
        }
        
        if (!string.IsNullOrWhiteSpace(configuration.AzureClientId))
        {
            _logger.LogTrace($"Using Client ID: {configuration.AzureClientId}");
            options.ClientId = configuration.AzureClientId;
        }

        _logger.LogTrace("WorkloadIdentityCredential will use environment variables: AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_FEDERATED_TOKEN_FILE");
        
        return new WorkloadIdentityCredential(options);
    }

    private TokenCredential CreateInteractiveBrowserCredential(AzureKeyVaultSignConfigurationSet configuration, Uri? authorityHost)
    {
        _logger.LogInformation("Creating InteractiveBrowserCredential for interactive browser authentication");
        
        InteractiveBrowserCredentialOptions options = new();
        
        if (authorityHost is not null)
        {
            options.AuthorityHost = authorityHost;
        }
        
        if (!string.IsNullOrWhiteSpace(configuration.AzureTenantId))
        {
            _logger.LogTrace($"Using Tenant ID: {configuration.AzureTenantId}");
            options.TenantId = configuration.AzureTenantId;
        }
        
        if (!string.IsNullOrWhiteSpace(configuration.AzureClientId))
        {
            _logger.LogTrace($"Using Client ID: {configuration.AzureClientId}");
            options.ClientId = configuration.AzureClientId;
        }

        _logger.LogInformation("Browser authentication will be required");
        
        return new InteractiveBrowserCredential(options);
    }

    private TokenCredential CreateEnvironmentCredential(Uri? authorityHost)
    {
        _logger.LogInformation("Creating EnvironmentCredential - will use environment variables for authentication");
        _logger.LogTrace("EnvironmentCredential will check environment variables: AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET or AZURE_CLIENT_CERTIFICATE_PATH");
        
        if (authorityHost is not null)
        {
            EnvironmentCredentialOptions options = new()
            {
                AuthorityHost = authorityHost
            };
            return new EnvironmentCredential(options);
        }
        return new EnvironmentCredential();
    }

    private TokenCredential CreateAzureCliCredential()
    {
        _logger.LogInformation("Creating AzureCliCredential - will use Azure CLI for authentication");
        _logger.LogTrace("Ensure you are logged in via 'az login' command");
        return new AzureCliCredential();
    }

    private TokenCredential CreateAzurePowerShellCredential()
    {
        _logger.LogInformation("Creating AzurePowerShellCredential - will use Azure PowerShell for authentication");
        _logger.LogTrace("Ensure you are logged in via 'Connect-AzAccount' command");
        return new AzurePowerShellCredential();
    }

    private TokenCredential CreateClientSecretCredential(AzureKeyVaultSignConfigurationSet configuration, Uri? authorityHost)
    {
        _logger.LogInformation($"Creating ClientSecretCredential with Tenant ID: {configuration.AzureTenantId}, Client ID: {configuration.AzureClientId}");
        
        if (authorityHost is not null)
        {
            ClientSecretCredentialOptions options = new()
            {
                AuthorityHost = authorityHost
            };
            return new ClientSecretCredential(configuration.AzureTenantId, configuration.AzureClientId, configuration.AzureClientSecret, options);
        }
        return new ClientSecretCredential(configuration.AzureTenantId, configuration.AzureClientId, configuration.AzureClientSecret);
    }

    private TokenCredential CreateAccessTokenCredential(AzureKeyVaultSignConfigurationSet configuration)
    {
        _logger.LogInformation("Creating AccessTokenCredential using provided access token");
        return new AccessTokenCredential(configuration.AzureAccessToken);
    }
}
