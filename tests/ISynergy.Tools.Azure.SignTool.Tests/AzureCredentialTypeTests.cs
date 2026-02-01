using ISynergy.Tools.Azure.SignTool.Enumerations;

namespace ISynergy.Tools.Azure.SignTool.Tests;

/// <summary>
/// Tests for AzureCredentialType enumeration
/// </summary>
[TestClass]
public class AzureCredentialTypeTests
{
    [TestMethod]
    public void AllCredentialTypesShouldBeDefined()
    {
        // Arrange & Act
        var credentialTypes = Enum.GetValues<AzureCredentialType>();

        // Assert
        Assert.AreEqual(9, credentialTypes.Length, "Expected 9 credential types to be defined");
        Assert.IsTrue(credentialTypes.Contains(AzureCredentialType.DefaultAzureCredential));
        Assert.IsTrue(credentialTypes.Contains(AzureCredentialType.ManagedIdentityCredential));
        Assert.IsTrue(credentialTypes.Contains(AzureCredentialType.WorkloadIdentityCredential));
        Assert.IsTrue(credentialTypes.Contains(AzureCredentialType.InteractiveBrowserCredential));
        Assert.IsTrue(credentialTypes.Contains(AzureCredentialType.EnvironmentCredential));
        Assert.IsTrue(credentialTypes.Contains(AzureCredentialType.AzureCliCredential));
        Assert.IsTrue(credentialTypes.Contains(AzureCredentialType.AzurePowerShellCredential));
        Assert.IsTrue(credentialTypes.Contains(AzureCredentialType.ClientSecretCredential));
        Assert.IsTrue(credentialTypes.Contains(AzureCredentialType.AccessTokenCredential));
    }

    [TestMethod]
    public void CredentialTypeNamesShouldMatchExpectedValues()
    {
        // Assert
        Assert.AreEqual("DefaultAzureCredential", AzureCredentialType.DefaultAzureCredential.ToString());
        Assert.AreEqual("ManagedIdentityCredential", AzureCredentialType.ManagedIdentityCredential.ToString());
        Assert.AreEqual("WorkloadIdentityCredential", AzureCredentialType.WorkloadIdentityCredential.ToString());
        Assert.AreEqual("InteractiveBrowserCredential", AzureCredentialType.InteractiveBrowserCredential.ToString());
        Assert.AreEqual("EnvironmentCredential", AzureCredentialType.EnvironmentCredential.ToString());
        Assert.AreEqual("AzureCliCredential", AzureCredentialType.AzureCliCredential.ToString());
        Assert.AreEqual("AzurePowerShellCredential", AzureCredentialType.AzurePowerShellCredential.ToString());
        Assert.AreEqual("ClientSecretCredential", AzureCredentialType.ClientSecretCredential.ToString());
        Assert.AreEqual("AccessTokenCredential", AzureCredentialType.AccessTokenCredential.ToString());
    }

    [TestMethod]
    [DataRow("DefaultAzureCredential", AzureCredentialType.DefaultAzureCredential)]
    [DataRow("ManagedIdentityCredential", AzureCredentialType.ManagedIdentityCredential)]
    [DataRow("WorkloadIdentityCredential", AzureCredentialType.WorkloadIdentityCredential)]
    [DataRow("InteractiveBrowserCredential", AzureCredentialType.InteractiveBrowserCredential)]
    [DataRow("EnvironmentCredential", AzureCredentialType.EnvironmentCredential)]
    [DataRow("AzureCliCredential", AzureCredentialType.AzureCliCredential)]
    [DataRow("AzurePowerShellCredential", AzureCredentialType.AzurePowerShellCredential)]
    [DataRow("ClientSecretCredential", AzureCredentialType.ClientSecretCredential)]
    [DataRow("AccessTokenCredential", AzureCredentialType.AccessTokenCredential)]
    public void EnumParseShouldWorkCaseInsensitive(string input, AzureCredentialType expected)
    {
        // Act
        bool result = Enum.TryParse<AzureCredentialType>(input, ignoreCase: true, out var parsed);

        // Assert
        Assert.IsTrue(result, $"Failed to parse '{input}'");
        Assert.AreEqual(expected, parsed);
    }

    [TestMethod]
    [DataRow("defaultazurecredential")]
    [DataRow("MANAGEDIDENTITYCREDENTIAL")]
    [DataRow("workloadidentitycredential")]
    public void EnumParseShouldBeCaseInsensitive(string input)
    {
        // Act
        bool result = Enum.TryParse<AzureCredentialType>(input, ignoreCase: true, out var parsed);

        // Assert
        Assert.IsTrue(result, $"Failed to parse '{input}' case-insensitively");
    }

    [TestMethod]
    [DataRow("InvalidCredential")]
    [DataRow("")]
    [DataRow("SomeRandomValue")]
    public void EnumParseShouldFailForInvalidValues(string input)
    {
        // Act
        bool result = Enum.TryParse<AzureCredentialType>(input, ignoreCase: true, out _);

        // Assert
        Assert.IsFalse(result, $"Expected parsing to fail for '{input}'");
    }
}
