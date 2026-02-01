# ISynergy.Tools.Azure.SignTool

A command-line tool for signing files using Azure Key Vault certificates. Similar to `signtool` in the Windows SDK, but uses Azure Key Vault for performing the signing process.

## Installation

```powershell
dotnet tool install --global ISynergy.Tools.Azure.SignTool
```

## Usage

### Basic Usage with Client Secret
```powershell
AzureSignTool sign -kvu https://my-vault.vault.azure.net -kvc my-cert -kvi <client-id> -kvt <tenant-id> -kvs <client-secret> -tr http://timestamp.digicert.com myfile.exe
```

### Recommended: Using Explicit Credential Type (Production)
```powershell
AzureSignTool sign -kvu https://my-vault.vault.azure.net -kvc my-cert -act WorkloadIdentityCredential -tr http://timestamp.digicert.com myfile.exe
```

Use `--help` or `sign --help` for detailed parameter information.

For production scenarios, it's recommended to use `--azure-credential-type` to explicitly specify which authentication method to use instead of relying on DefaultAzureCredential. See the main README for more details.

## Features

- Sign files using Azure Key Vault certificates
- Support for RFC3161 and legacy Authenticode timestamps
- Parallel signing of multiple files
- Support for APPX/MSIX packages and NuGet packages
- Multiple authentication methods including:
  - Managed Identity
  - Workload Identity (for Kubernetes/containers)
  - Client Secret
  - Interactive Browser
  - Azure CLI
  - Azure PowerShell
  - Environment variables
  - Access tokens
- Explicit credential type selection for production scenarios (recommended over DefaultAzureCredential)
