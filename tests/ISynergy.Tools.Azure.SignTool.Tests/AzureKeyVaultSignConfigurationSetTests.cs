using ISynergy.Tools.Azure.SignTool.Configuration;
using ISynergy.Tools.Azure.SignTool.Enumerations;

namespace ISynergy.Tools.Azure.SignTool.Tests;

/// <summary>
/// Tests for AzureKeyVaultSignConfigurationSet
/// </summary>
[TestClass]
public class AzureKeyVaultSignConfigurationSetTests
{
    [TestMethod]
    public void ConfigurationSet_ShouldAcceptAllProperties()
    {
        // Arrange
        var testUrl = new Uri("https://test.vault.azure.net");

        // Act
        var config = new AzureKeyVaultSignConfigurationSet
        {
            AzureKeyVaultUrl = testUrl,
            AzureKeyVaultCertificateName = "test-cert",
            AzureKeyVaultCertificateVersion = "v1",
            AzureClientId = "client-id",
            AzureClientSecret = "client-secret",
            AzureTenantId = "tenant-id",
            AzureAccessToken = "access-token",
            AzureAuthority = "public",
            ManagedIdentity = false,
            AzureCredentialType = AzureCredentialType.WorkloadIdentityCredential
        };

        // Assert
        Assert.AreEqual(testUrl, config.AzureKeyVaultUrl);
        Assert.AreEqual("test-cert", config.AzureKeyVaultCertificateName);
        Assert.AreEqual("v1", config.AzureKeyVaultCertificateVersion);
        Assert.AreEqual("client-id", config.AzureClientId);
        Assert.AreEqual("client-secret", config.AzureClientSecret);
        Assert.AreEqual("tenant-id", config.AzureTenantId);
        Assert.AreEqual("access-token", config.AzureAccessToken);
        Assert.AreEqual("public", config.AzureAuthority);
        Assert.IsFalse(config.ManagedIdentity);
        Assert.AreEqual(AzureCredentialType.WorkloadIdentityCredential, config.AzureCredentialType);
    }

    [TestMethod]
    public void ConfigurationSet_WithNullCredentialType_ShouldAllowNull()
    {
        // Arrange & Act
        var config = new AzureKeyVaultSignConfigurationSet
        {
            AzureKeyVaultUrl = new Uri("https://test.vault.azure.net"),
            AzureKeyVaultCertificateName = "test-cert",
            AzureCredentialType = null
        };

        // Assert
        Assert.IsNull(config.AzureCredentialType);
    }

    [TestMethod]
    [DataRow(AzureCredentialType.WorkloadIdentityCredential)]
    [DataRow(AzureCredentialType.ManagedIdentityCredential)]
    [DataRow(AzureCredentialType.ClientSecretCredential)]
    [DataRow(AzureCredentialType.AccessTokenCredential)]
    [DataRow(AzureCredentialType.EnvironmentCredential)]
    [DataRow(AzureCredentialType.AzureCliCredential)]
    [DataRow(AzureCredentialType.AzurePowerShellCredential)]
    [DataRow(AzureCredentialType.InteractiveBrowserCredential)]
    [DataRow(AzureCredentialType.DefaultAzureCredential)]
    public void ConfigurationSet_ShouldAcceptAllCredentialTypes(AzureCredentialType credentialType)
    {
        // Arrange & Act
        var config = new AzureKeyVaultSignConfigurationSet
        {
            AzureKeyVaultUrl = new Uri("https://test.vault.azure.net"),
            AzureKeyVaultCertificateName = "test-cert",
            AzureCredentialType = credentialType
        };

        // Assert
        Assert.AreEqual(credentialType, config.AzureCredentialType);
    }

    [TestMethod]
    public void ConfigurationSet_WithManagedIdentityTrue_ShouldWork()
    {
        // Arrange & Act
        var config = new AzureKeyVaultSignConfigurationSet
        {
            AzureKeyVaultUrl = new Uri("https://test.vault.azure.net"),
            AzureKeyVaultCertificateName = "test-cert",
            ManagedIdentity = true,
            AzureCredentialType = null
        };

        // Assert
        Assert.IsTrue(config.ManagedIdentity);
        Assert.IsNull(config.AzureCredentialType);
    }

    [TestMethod]
    public void ConfigurationSet_CanSetBothManagedIdentityAndCredentialType()
    {
        // Arrange & Act
        var config = new AzureKeyVaultSignConfigurationSet
        {
            AzureKeyVaultUrl = new Uri("https://test.vault.azure.net"),
            AzureKeyVaultCertificateName = "test-cert",
            ManagedIdentity = true,
            AzureCredentialType = AzureCredentialType.WorkloadIdentityCredential
        };

        // Assert - Configuration allows this, validation happens elsewhere
        Assert.IsTrue(config.ManagedIdentity);
        Assert.AreEqual(AzureCredentialType.WorkloadIdentityCredential, config.AzureCredentialType);
    }

    [TestMethod]
    public void ConfigurationSet_WithMinimalProperties_ShouldWork()
    {
        // Arrange & Act
        var config = new AzureKeyVaultSignConfigurationSet
        {
            AzureKeyVaultUrl = new Uri("https://test.vault.azure.net"),
            AzureKeyVaultCertificateName = "test-cert",
            AzureCredentialType = AzureCredentialType.AzureCliCredential
        };

        // Assert
        Assert.IsNotNull(config.AzureKeyVaultUrl);
        Assert.IsNotNull(config.AzureKeyVaultCertificateName);
        Assert.AreEqual(AzureCredentialType.AzureCliCredential, config.AzureCredentialType);
        Assert.IsNull(config.AzureClientId);
        Assert.IsNull(config.AzureClientSecret);
        Assert.IsNull(config.AzureTenantId);
    }
}
