# ISynergy.Tools.Azure.SignTool

A code signing tool that uses Azure Key Vault for performing the signing process. Similar to `signtool` in the Windows SDK, but uses Azure Key Vault certificates.

This project is based on [AzureSignTool](https://github.com/vcsjones/AzureSignTool) by Kevin Jones.

## Example Usage

### Basic Usage with Client Secret

```powershell
ISynergy.Tools.Azure.SignTool sign -du "https://example.com" `
    -fd sha384 -kvu https://my-vault.vault.azure.net `
    -kvi 01234567-abcd-ef012-0000-0123456789ab `
    -kvt 01234567-abcd-ef012-0000-0123456789ab `
    -kvs <client-secret> `
    -kvc my-certificate-name `
    -tr http://timestamp.digicert.com `
    -td sha384 `
    -v `
    myfile.exe
```

### Using Workload Identity (Recommended for Kubernetes/Containers)

```powershell
ISynergy.Tools.Azure.SignTool sign -du "https://example.com" `
    -fd sha384 -kvu https://my-vault.vault.azure.net `
    -act WorkloadIdentityCredential `
    -kvc my-certificate-name `
    -tr http://timestamp.digicert.com `
    -td sha384 `
    -v `
    myfile.exe
```

### Using Managed Identity

```powershell
ISynergy.Tools.Azure.SignTool sign -du "https://example.com" `
    -fd sha384 -kvu https://my-vault.vault.azure.net `
    -act ManagedIdentityCredential `
    -kvc my-certificate-name `
    -tr http://timestamp.digicert.com `
    -td sha384 `
    -v `
    myfile.exe
```

### Using Azure CLI Credential (for Local Development)

```powershell
ISynergy.Tools.Azure.SignTool sign -du "https://example.com" `
    -fd sha384 -kvu https://my-vault.vault.azure.net `
    -act AzureCliCredential `
    -kvc my-certificate-name `
    -tr http://timestamp.digicert.com `
    -td sha384 `
    -v `
    myfile.exe
```

### Using Interactive Browser (for Interactive Scenarios)

```powershell
ISynergy.Tools.Azure.SignTool sign -du "https://example.com" `
    -fd sha384 -kvu https://my-vault.vault.azure.net `
    -act InteractiveBrowserCredential `
    -kvi 01234567-abcd-ef012-0000-0123456789ab `
    -kvt 01234567-abcd-ef012-0000-0123456789ab `
    -kvc my-certificate-name `
    -tr http://timestamp.digicert.com `
    -td sha384 `
    -v `
    myfile.exe
```

The `--help` or `sign --help` option provides more detail about each parameter.

## Installation

### .NET Tool

```powershell
dotnet tool install --global ISynergy.Tools.Azure.SignTool
```

## Authentication Methods

This tool supports multiple Azure authentication methods. For **production scenarios**, it is recommended to use the `--azure-credential-type` parameter to explicitly specify which credential type to use, rather than relying on `DefaultAzureCredential`.

### Authentication Logging

When running the tool, it will log which authentication method is being used. This helps with troubleshooting authentication issues:

- Use `--verbose` or `-v` to see detailed authentication information
- The tool will log the credential type being used (e.g., "Using explicitly specified credential type: WorkloadIdentityCredential")
- For verbose output, you'll see additional details like environment variables being checked or configuration parameters being used

Example with verbose logging:
```powershell
ISynergy.Tools.Azure.SignTool sign -kvu https://my-vault.vault.azure.net `
    -act WorkloadIdentityCredential `
    -kvc my-certificate `
    -tr http://timestamp.digicert.com `
    -v `
    myfile.exe
```

### Why Use `--azure-credential-type`?

According to [Microsoft's documentation](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential), `DefaultAzureCredential` is designed to simplify authentication during development but is **not recommended for production** scenarios. Using `--azure-credential-type` allows you to:

- **Explicitly control** which authentication method is used
- **Improve security** by limiting the credential discovery chain
- **Better performance** by avoiding unnecessary authentication attempts
- **Easier troubleshooting** with predictable authentication flow

### Recommended Credential Types by Scenario

| Scenario | Recommended Credential Type | Example |
|----------|----------------------------|---------|
| **Kubernetes/Containers** | `WorkloadIdentityCredential` | CI/CD pipelines in AKS with workload identity |
| **Azure VMs/App Services** | `ManagedIdentityCredential` | Applications running on Azure compute resources |
| **CI/CD with Service Principal** | `ClientSecretCredential` | Automated builds with explicit credentials |
| **Local Development** | `AzureCliCredential` | Developers using Azure CLI authentication |
| **Interactive Scenarios** | `InteractiveBrowserCredential` | Manual signing operations requiring user login |
| **Environment Variables** | `EnvironmentCredential` | Applications using Azure environment variables |

## Parameters

### Azure Key Vault Authentication

* `--azure-key-vault-url` [short: `-kvu`, required: yes]: A fully qualified URL of the key vault with the certificate that will be used for signing. Example: `https://my-vault.vault.azure.net`.

* `--azure-key-vault-client-id` [short: `-kvi`, required: possibly]: The client ID used to authenticate to Azure. Required if using client credentials authentication. Must be supplied with `--azure-key-vault-client-secret` and `--azure-key-vault-tenant-id`.

* `--azure-key-vault-client-secret` [short: `-kvs`, required: possibly]: The client secret used to authenticate to Azure. Required if using client credentials authentication.

* `--azure-key-vault-tenant-id` [short: `-kvt`, required: possibly]: The tenant ID used to authenticate to Azure. Required if using client credentials authentication.

* `--azure-key-vault-certificate` [short: `-kvc`, required: yes]: The name of the certificate used to perform the signing operation.

* `--azure-key-vault-certificate-version` [short: `-kvcv`, required: no]: The specific version of the certificate to use. If not specified, the latest version is used.

* `--azure-key-vault-accesstoken` [short: `-kva`, required: possibly]: An access token used to authenticate to Azure. Use this instead of client credentials when the calling application already has an access token.

* `--azure-key-vault-managed-identity` [short: `-kvm`, required: possibly]: Use Azure Managed Identity to authenticate. This option uses [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) which supports Managed Identity, Azure CLI, PowerShell, Visual Studio credentials, and more. **Note:** DefaultAzureCredential is not recommended for production scenarios - use `--azure-credential-type` instead for explicit credential selection.

* `--azure-credential-type` [short: `-act`, required: possibly]: **Recommended for production.** Specify the exact type of Azure credential to use for authentication. This allows you to avoid using DefaultAzureCredential and explicitly control which authentication method is used. Allowed values:
  - `DefaultAzureCredential` - Tries multiple credential types in sequence (not recommended for production)
  - `ManagedIdentityCredential` - Use Azure Managed Identity
  - `WorkloadIdentityCredential` - Use workload identity federation (recommended for Kubernetes/containerized workloads)
  - `InteractiveBrowserCredential` - Interactive browser authentication
  - `EnvironmentCredential` - Authenticate using environment variables
  - `AzureCliCredential` - Authenticate using Azure CLI
  - `AzurePowerShellCredential` - Authenticate using Azure PowerShell
  - `ClientSecretCredential` - Authenticate using client secret (requires `--azure-key-vault-client-id`, `--azure-key-vault-client-secret`, and `--azure-key-vault-tenant-id`)
  - `AccessTokenCredential` - Authenticate using an access token (requires `--azure-key-vault-accesstoken`)

* `--azure-authority` [short: `-au`, required: no]: The Azure Authority for Azure Key Vault (for sovereign clouds).

### Signing Options

* `--description` [short: `-d`, required: no]: A description of the signed content. Same as `/d` in Windows SDK `signtool`.

* `--description-url` [short: `-du`, required: no]: A URL with more information about the signed content. Same as `/du` in Windows SDK `signtool`.

* `--timestamp-rfc3161` [short: `-tr`, required: no]: A URL to an RFC3161 compliant timestamping service. Recommended over `--timestamp-authenticode`.

* `--timestamp-authenticode` [short: `-t`, required: no]: A URL to a legacy Authenticode timestamping service. Deprecated - use `--timestamp-rfc3161` instead.

* `--timestamp-digest` [short: `-td`, required: no]: The digest algorithm used for timestamping. Default: `sha256`. Values: sha1, sha256, sha384, sha512.

* `--file-digest` [short: `-fd`, required: no]: The digest algorithm used for hashing the file. Default: `sha256`. Values: sha1, sha256, sha384, sha512.

* `--additional-certificates` [short: `-ac`, required: no]: Paths to additional certificates to include in the certificate chain. Can be specified multiple times.

### Output Options

* `--verbose` [short: `-v`, required: no]: Include additional output in the log. **Recommended for troubleshooting authentication issues** - shows which credential type is being used and configuration details.

* `--quiet` [short: `-q`, required: no]: Do not print output to the log.

* `--colors` [required: no]: Enable color output on the command line.

### Processing Options

* `--continue-on-error` [short: `-coe`, required: no]: Continue signing when a file fails instead of stopping.

* `--input-file-list` [short: `-ifl`, required: no]: Path to a text file containing a list of files to sign, one per line.

* `--skip-signed` [short: `-s`, required: no]: Skip files that are already signed.

* `--append-signature` [short: `-as`, required: no]: Append signature instead of replacing existing signature. Requires Windows 11 or later.

* `--max-degree-of-parallelism` [short: `-mdop`, required: no]: Maximum number of concurrent signing operations. Default: 4.

### Advanced Options

* `--page-hashing` [short: `-ph`, required: no]: Generate page hashes for executable files.

* `--no-page-hashing` [short: `-nph`, required: no]: Suppress page hashes for executable files.

## Supported Formats

This tool uses the same mechanisms for signing as the Windows SDK `signtool` and supports the same formats including:
- Portable Executable (PE) files (.exe, .dll)
- Cabinet files (.cab)
- MSIX/APPX packages (.msix, .appx, .msixbundle, .appxbundle)
- **NuGet packages (.nupkg, .snupkg)** - Signs the binaries contained within the package

### NuGet Package Signing

When signing NuGet packages, the tool:
1. Automatically detects .nupkg files and looks for corresponding .snupkg (symbol package) files
2. Extracts the package contents
3. Signs all executable binaries inside (.dll, .exe, .winmd)
4. Repackages the signed files back into the original package format

Example:
```powershell
ISynergy.Tools.Azure.SignTool sign -kvu https://my-vault.vault.azure.net `
    -kvi <client-id> -kvt <tenant-id> -kvs <client-secret> `
    -kvc my-certificate `
    -tr http://timestamp.digicert.com `
    MyPackage.1.0.0.nupkg
```

If `MyPackage.1.0.0.snupkg` exists in the same directory, it will automatically be signed as well.
- Windows Installer packages (.msi, .msix)
- APPX/MSIX packages

## Exit Codes

| Code | Description |
|------|-------------|
| 0 (S_OK) | All files signed successfully |
| 0x20000001 | Some files signed successfully, some failed |
| 0xA0000002 | All files failed to sign |

## Troubleshooting

### Authentication Issues

If you're having trouble with authentication, use the `--verbose` flag to see which credential type is being used:

```powershell
ISynergy.Tools.Azure.SignTool sign -kvu https://my-vault.vault.azure.net `
    -act WorkloadIdentityCredential `
    -kvc my-certificate `
    -v `
    myfile.exe
```

The output will show:
- Which credential type is being used (e.g., "Using explicitly specified credential type: WorkloadIdentityCredential")
- Configuration parameters (Tenant ID, Client ID, etc.)
- Environment variables being checked (for EnvironmentCredential, WorkloadIdentityCredential, etc.)
- Warnings if using DefaultAzureCredential in production

### Common Scenarios

#### WorkloadIdentityCredential (Kubernetes)
Ensure these environment variables are set:
- `AZURE_TENANT_ID`
- `AZURE_CLIENT_ID`
- `AZURE_FEDERATED_TOKEN_FILE`

#### ManagedIdentityCredential (Azure VMs/App Services)
The Azure resource must have a managed identity assigned with appropriate permissions to the Key Vault.

#### AzureCliCredential
Ensure you're logged in: `az login`

#### AzurePowerShellCredential
Ensure you're logged in: `Connect-AzAccount`

## Requirements

- Windows 10 or later
- .NET 10.0 SDK (for building)

## License

This project is licensed under the MIT License.

## Acknowledgments

This project is based on [AzureSignTool](https://github.com/vcsjones/AzureSignTool) by Kevin Jones (vcsjones).
