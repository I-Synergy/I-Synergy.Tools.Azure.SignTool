# ISynergy.Tools.Azure.SignTool.Core

Core library for Azure Sign Tool that provides the signing functionality using Azure Key Vault certificates.

## Features

- Authenticode signing using Azure Key Vault
- Support for RFC3161 and legacy Authenticode timestamps
- Support for various file formats (PE, APPX, MSIX)
- Parallel signing operations

## Usage

This library is used internally by the ISynergy.Tools.Azure.SignTool CLI tool. For direct usage, reference the package and use the `AuthenticodeKeyVaultSigner` class.
