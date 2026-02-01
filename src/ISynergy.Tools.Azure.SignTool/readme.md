# 🔐 I-Synergy Azure SignTool

A code signing tool that uses **Azure Key Vault** for performing the signing process. Similar to `signtool` in the Windows SDK, but leverages Azure Key Vault certificates for secure, centralized key management.

> 📦 Based on [AzureSignTool](https://github.com/vcsjones/AzureSignTool) by Kevin Jones.

---

## 📑 Table of Contents

- [⚡ Quick Start](#-quick-start)
- [📥 Installation](#-installation)
- [🔑 Authentication Methods](#-authentication-methods)
- [💻 Usage Examples](#-usage-examples)
- [📖 Command Reference](#-command-reference)
- [📁 Supported Formats](#-supported-formats)
- [🚦 Exit Codes](#-exit-codes)
- [🔧 Troubleshooting](#-troubleshooting)
- [📋 Requirements](#-requirements)
- [📄 License](#-license)

---

## ⚡ Quick Start

```powershell
# Install the tool
dotnet tool install --global I-Synergy.Tools.Azure.SignTool

# Sign a file using Azure CLI credentials (for local development)
AzureSignTool sign `
    -kvu https://my-vault.vault.azure.net `
    -kvc my-certificate-name `
    -act AzureCliCredential `
    -tr http://timestamp.digicert.com `
    myfile.exe
```

---

## 📥 Installation

Install as a global .NET tool:

```powershell
dotnet tool install --global I-Synergy.Tools.Azure.SignTool
```

---

## 🔑 Authentication Methods

This tool supports multiple Azure authentication methods. For **production scenarios**, use the `--azure-credential-type` (`-act`) parameter to explicitly specify the credential type rather than relying on `DefaultAzureCredential`.

### 🎯 Choosing the Right Credential Type

| Scenario | Credential Type | When to Use |
|:---------|:----------------|:------------|
| ☸️ Kubernetes / Containers | `WorkloadIdentityCredential` | CI/CD pipelines in AKS with workload identity federation |
| ☁️ Azure VMs / App Services | `ManagedIdentityCredential` | Applications running on Azure compute resources |
| 🔄 CI/CD with Service Principal | `ClientSecretCredential` | Automated builds with explicit client credentials |
| 🖥️ Local Development | `AzureCliCredential` | Developers authenticated via `az login` |
| 🌐 Interactive Use | `InteractiveBrowserCredential` | Manual signing operations requiring user login |
| 🔧 Environment Variables | `EnvironmentCredential` | Applications configured via Azure environment variables |

### ⚠️ Why Avoid `DefaultAzureCredential` in Production?

According to [Microsoft's documentation](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential), `DefaultAzureCredential` is designed for development convenience but is **not recommended for production** because it:

- ❌ Tries multiple credential types in sequence, causing unpredictable behavior
- ❌ May attempt unnecessary authentication methods, impacting performance
- ❌ Makes troubleshooting more difficult due to its "magic" discovery chain

Using `--azure-credential-type` provides:

- ✅ **Explicit control** over which authentication method is used
- ✅ **Better security** by limiting credential discovery
- ✅ **Improved performance** by avoiding unnecessary auth attempts
- ✅ **Easier troubleshooting** with predictable authentication flow

---

## 💻 Usage Examples

### 🖥️ Using Azure CLI Credential (Local Development)

```powershell
AzureSignTool sign -du "https://example.com" `
    -fd sha384 `
    -kvu https://my-vault.vault.azure.net `
    -act AzureCliCredential `
    -kvc my-certificate-name `
    -tr http://timestamp.digicert.com `
    -td sha384 `
    -v `
    myfile.exe
```

### ☸️ Using Workload Identity (Kubernetes / Containers)

```powershell
AzureSignTool sign -du "https://example.com" `
    -fd sha384 `
    -kvu https://my-vault.vault.azure.net `
    -act WorkloadIdentityCredential `
    -kvc my-certificate-name `
    -tr http://timestamp.digicert.com `
    -td sha384 `
    -v `
    myfile.exe
```

### ☁️ Using Managed Identity (Azure VMs / App Services)

```powershell
AzureSignTool sign -du "https://example.com" `
    -fd sha384 `
    -kvu https://my-vault.vault.azure.net `
    -act ManagedIdentityCredential `
    -kvc my-certificate-name `
    -tr http://timestamp.digicert.com `
    -td sha384 `
    -v `
    myfile.exe
```

### 🔄 Using Client Secret (CI/CD Pipelines)

```powershell
AzureSignTool sign -du "https://example.com" `
    -fd sha384 `
    -kvu https://my-vault.vault.azure.net `
    -kvi 01234567-abcd-ef012-0000-0123456789ab `
    -kvt 01234567-abcd-ef012-0000-0123456789ab `
    -kvs <client-secret> `
    -kvc my-certificate-name `
    -tr http://timestamp.digicert.com `
    -td sha384 `
    -v `
    myfile.exe
```

### 🌐 Using Interactive Browser

```powershell
AzureSignTool sign -du "https://example.com" `
    -fd sha384 `
    -kvu https://my-vault.vault.azure.net `
    -act InteractiveBrowserCredential `
    -kvi 01234567-abcd-ef012-0000-0123456789ab `
    -kvt 01234567-abcd-ef012-0000-0123456789ab `
    -kvc my-certificate-name `
    -tr http://timestamp.digicert.com `
    -td sha384 `
    -v `
    myfile.exe
```

> 💡 **Tip:** Use `--help` or `sign --help` for detailed parameter information.

---

## 📖 Command Reference

### 🔐 Azure Key Vault Authentication

| Parameter | Short | Required | Description |
|:----------|:-----:|:--------:|:------------|
| `--azure-key-vault-url` | `-kvu` | ✅ | Fully qualified URL of the key vault (e.g., `https://my-vault.vault.azure.net`) |
| `--azure-key-vault-certificate` | `-kvc` | ✅ | Name of the certificate for signing |
| `--azure-key-vault-certificate-version` | `-kvcv` | ➖ | Specific certificate version (defaults to latest) |
| `--azure-key-vault-client-id` | `-kvi` | ⚙️ | Client ID for service principal authentication |
| `--azure-key-vault-client-secret` | `-kvs` | ⚙️ | Client secret for service principal authentication |
| `--azure-key-vault-tenant-id` | `-kvt` | ⚙️ | Tenant ID for service principal authentication |
| `--azure-key-vault-accesstoken` | `-kva` | ⚙️ | Pre-acquired access token for authentication |
| `--azure-key-vault-managed-identity` | `-kvm` | ⚙️ | Use DefaultAzureCredential (not recommended for production) |
| `--azure-credential-type` | `-act` | ⚙️ | **Recommended.** Explicit credential type (see [Authentication Methods](#-authentication-methods)) |
| `--azure-authority` | `-au` | ➖ | Azure Authority URL (for sovereign clouds) |

> ✅ = Required | ➖ = Optional | ⚙️ = Required depending on authentication method

#### 🎛️ Available Credential Types (`-act`)

| Value | Description |
|:------|:------------|
| `DefaultAzureCredential` | 🔄 Tries multiple credential types in sequence (not recommended for production) |
| `ManagedIdentityCredential` | ☁️ Azure Managed Identity |
| `WorkloadIdentityCredential` | ☸️ Workload identity federation (Kubernetes/containers) |
| `ClientSecretCredential` | 🔑 Service principal with client secret |
| `InteractiveBrowserCredential` | 🌐 Interactive browser authentication |
| `EnvironmentCredential` | 🔧 Authenticate using environment variables |
| `AzureCliCredential` | 🖥️ Authenticate using Azure CLI |
| `AzurePowerShellCredential` | 💠 Authenticate using Azure PowerShell |
| `AccessTokenCredential` | 🎫 Use a pre-acquired access token |

### ✍️ Signing Options

| Parameter | Short | Default | Description |
|:----------|:-----:|:-------:|:------------|
| `--description` | `-d` | ➖ | Description of the signed content (same as `/d` in signtool) |
| `--description-url` | `-du` | ➖ | URL with more information about the content (same as `/du` in signtool) |
| `--file-digest` | `-fd` | sha256 | Hash algorithm for file digest: `sha1`, `sha256`, `sha384`, `sha512` |
| `--timestamp-rfc3161` | `-tr` | ➖ | RFC3161 timestamp server URL (recommended) |
| `--timestamp-authenticode` | `-t` | ➖ | Legacy Authenticode timestamp server (deprecated) |
| `--timestamp-digest` | `-td` | sha256 | Hash algorithm for timestamp: `sha1`, `sha256`, `sha384`, `sha512` |
| `--additional-certificates` | `-ac` | ➖ | Additional certificates for the chain (can be specified multiple times) |

### ⚙️ Processing Options

| Parameter | Short | Default | Description |
|:----------|:-----:|:-------:|:------------|
| `--continue-on-error` | `-coe` | ❌ | Continue signing remaining files if one fails |
| `--input-file-list` | `-ifl` | ➖ | Path to text file containing files to sign (one per line) |
| `--skip-signed` | `-s` | ❌ | Skip files that are already signed |
| `--append-signature` | `-as` | ❌ | Append signature instead of replacing (Windows 11+) |
| `--max-degree-of-parallelism` | `-mdop` | 4 | Maximum concurrent signing operations |
| `--sign-nuget-contents` | `-snc` | ❌ | Sign binaries inside NuGet packages |
| `--sign-nuget-contents-filter` | `-sncf` | ➖ | Glob pattern to filter which binaries to sign inside NuGet packages |

### 📤 Output Options

| Parameter | Short | Description |
|:----------|:-----:|:------------|
| `--verbose` | `-v` | 📝 Show detailed output including authentication info |
| `--quiet` | `-q` | 🔇 Suppress all output |
| `--colors` | ➖ | 🎨 Enable colored output |

### 🔬 Advanced Options

| Parameter | Short | Description |
|:----------|:-----:|:------------|
| `--page-hashing` | `-ph` | Generate page hashes for executable files |
| `--no-page-hashing` | `-nph` | Suppress page hashes for executable files |

---

## 📁 Supported Formats

This tool supports the same formats as the Windows SDK `signtool`:

| Format | Extensions |
|:-------|:-----------|
| 🖥️ Portable Executable | `.exe`, `.dll` |
| 📦 Cabinet Files | `.cab` |
| 💿 Windows Installer | `.msi`, `.msix` |
| 📱 APPX/MSIX Packages | `.appx`, `.msix`, `.appxbundle`, `.msixbundle` |
| 📦 NuGet Packages | `.nupkg`, `.snupkg` |

### 📦 NuGet Package Signing

When signing NuGet packages, the tool supports **two types of signing**:

#### 1️⃣ Package-Level Signature (Default)

Creates a NuGet author signature (`.signature.p7s`) for the package itself.

```powershell
AzureSignTool sign `
    -kvu https://my-vault.vault.azure.net `
    -kvi <client-id> -kvt <tenant-id> -kvs <client-secret> `
    -kvc my-certificate `
    -tr http://timestamp.digicert.com `
    MyPackage.1.0.0.nupkg
```

#### 2️⃣ Package + Content Signing (Optional)

Signs both the package and all binaries inside it (`.dll`, `.exe`, `.winmd`).

```powershell
AzureSignTool sign `
    -kvu https://my-vault.vault.azure.net `
    -kvi <client-id> -kvt <tenant-id> -kvs <client-secret> `
    -kvc my-certificate `
    -tr http://timestamp.digicert.com `
    --sign-nuget-contents `
    MyPackage.1.0.0.nupkg
```

#### 3️⃣ Filtered Content Signing

Sign only specific binaries inside the package using a glob pattern (useful when packages contain third-party DLLs that are already signed).

```powershell
AzureSignTool sign `
    -kvu https://my-vault.vault.azure.net `
    -kvi <client-id> -kvt <tenant-id> -kvs <client-secret> `
    -kvc my-certificate `
    -tr http://timestamp.digicert.com `
    --sign-nuget-contents `
    --sign-nuget-contents-filter "ISynergy.Framework.*" `
    MyPackage.1.0.0.nupkg
```

> ℹ️ **Note:** Symbol packages (`.snupkg`) do not support package-level signatures per NuGet specifications. The certificate must have the Code Signing enhanced key usage (EKU).

---

## 🚦 Exit Codes

| Code | Name | Description |
|:----:|:-----|:------------|
| `0` | ✅ S_OK | All files signed successfully |
| `0x20000001` | ⚠️ Partial Success | Some files signed, some failed |
| `0xA0000002` | ❌ All Failed | All files failed to sign |

---

## 🔧 Troubleshooting

### 📝 Enable Verbose Logging

Use the `--verbose` (`-v`) flag to see detailed authentication and signing information:

```powershell
AzureSignTool sign `
    -kvu https://my-vault.vault.azure.net `
    -act WorkloadIdentityCredential `
    -kvc my-certificate `
    -v `
    myfile.exe
```

The verbose output includes:
- 🔑 Which credential type is being used
- ⚙️ Configuration parameters (Tenant ID, Client ID, etc.)
- 🔧 Environment variables being checked
- ⚠️ Warnings if using `DefaultAzureCredential` in production

### 🔍 Common Authentication Issues

#### ☸️ WorkloadIdentityCredential (Kubernetes)

Ensure these environment variables are set:

| Variable | Description |
|:---------|:------------|
| `AZURE_TENANT_ID` | 🏢 Azure AD tenant ID |
| `AZURE_CLIENT_ID` | 🆔 Application (client) ID |
| `AZURE_FEDERATED_TOKEN_FILE` | 📄 Path to the projected service account token |

#### ☁️ ManagedIdentityCredential (Azure VMs / App Services)

- ✅ Verify the Azure resource has a managed identity assigned
- ✅ Ensure the identity has appropriate Key Vault access policies or RBAC permissions

#### 🖥️ AzureCliCredential

```powershell
# Ensure you're logged in
az login

# Verify your account
az account show
```

#### 💠 AzurePowerShellCredential

```powershell
# Ensure you're logged in
Connect-AzAccount

# Verify your context
Get-AzContext
```

---

## 📋 Requirements

| Requirement | Version |
|:------------|:--------|
| 🖥️ Operating System | Windows 10 or later |
| 🛠️ .NET SDK (for building) | 10.0 |

---

## 📄 License

This project is licensed under the **MIT License**.

---

## 🙏 Acknowledgments

This project is based on [AzureSignTool](https://github.com/vcsjones/AzureSignTool) by Kevin Jones ([@vcsjones](https://github.com/vcsjones)).
