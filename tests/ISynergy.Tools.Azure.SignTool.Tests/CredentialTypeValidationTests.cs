namespace ISynergy.Tools.Azure.SignTool.Tests;

/// <summary>
/// Tests for credential type command-line argument validation
/// </summary>
[TestClass]
public class CredentialTypeValidationTests
{
    private static readonly SemaphoreSlim _sync = new(1, 1);

    [TestMethod]
    public async Task WhenNoAuthenticationMethodProvided_ShouldShowError()
    {
        // Act
        (string StdOut, string StdErr, int ExitCode) = await CaptureOutput(async () =>
        {
            return await Program.Main([
                "sign",
                "--azure-key-vault-url", "https://test.vault.azure.net",
                "--azure-key-vault-certificate", "test-cert",
                "test.exe"
            ]);
        });

        // Assert
        Assert.AreNotEqual(0, ExitCode, "Expected non-zero exit code");
        Assert.IsTrue(StdErr.Contains("One of '--azure-key-vault-accesstoken', '--azure-key-vault-client-id', '--azure-key-vault-managed-identity' or '--azure-credential-type' must be supplied"),
            $"Expected authentication method error, got: {StdErr}");
    }

    [TestMethod]
    public async Task WhenCredentialTypeProvidedWithManagedIdentityFlag_ShouldShowError()
    {
        // Act
        (string StdOut, string StdErr, int ExitCode) = await CaptureOutput(async () =>
        {
            return await Program.Main([
                "sign",
                "--azure-key-vault-url", "https://test.vault.azure.net",
                "--azure-key-vault-certificate", "test-cert",
                "--azure-key-vault-managed-identity",
                "--azure-credential-type", "WorkloadIdentityCredential",
                "test.exe"
            ]);
        });

        // Assert
        Assert.AreNotEqual(0, ExitCode, "Expected non-zero exit code");
        Assert.IsTrue(StdErr.Contains("Cannot use '--azure-credential-type' with '--azure-key-vault-accesstoken', '--azure-key-vault-client-id', or '--azure-key-vault-managed-identity'"),
            $"Expected mutual exclusion error, got: {StdErr}");
    }

    [TestMethod]
    public async Task WhenCredentialTypeProvidedWithClientId_ShouldShowError()
    {
        // Act
        (string StdOut, string StdErr, int ExitCode) = await CaptureOutput(async () =>
        {
            return await Program.Main([
                "sign",
                "--azure-key-vault-url", "https://test.vault.azure.net",
                "--azure-key-vault-certificate", "test-cert",
                "--azure-key-vault-client-id", "test-client-id",
                "--azure-key-vault-client-secret", "test-secret",
                "--azure-key-vault-tenant-id", "test-tenant",
                "--azure-credential-type", "WorkloadIdentityCredential",
                "test.exe"
            ]);
        });

        // Assert
        Assert.AreNotEqual(0, ExitCode, "Expected non-zero exit code");
        Assert.IsTrue(StdErr.Contains("Cannot use '--azure-credential-type' with '--azure-key-vault-accesstoken', '--azure-key-vault-client-id', or '--azure-key-vault-managed-identity'"),
            $"Expected mutual exclusion error, got: {StdErr}");
    }

    [TestMethod]
    public async Task WhenClientSecretCredentialWithoutClientId_ShouldShowError()
    {
        // Act
        (string StdOut, string StdErr, int ExitCode) = await CaptureOutput(async () =>
        {
            return await Program.Main([
                "sign",
                "--azure-key-vault-url", "https://test.vault.azure.net",
                "--azure-key-vault-certificate", "test-cert",
                "--azure-credential-type", "ClientSecretCredential",
                "test.exe"
            ]);
        });

        // Assert
        Assert.AreNotEqual(0, ExitCode, "Expected non-zero exit code");
        Assert.IsTrue(StdErr.Contains("'--azure-key-vault-client-id' is required when using ClientSecretCredential"),
            $"Expected client ID required error, got: {StdErr}");
    }

    [TestMethod]
    public async Task WhenClientSecretCredentialWithoutClientSecret_ShouldShowError()
    {
        // Act
        (string StdOut, string StdErr, int ExitCode) = await CaptureOutput(async () =>
        {
            return await Program.Main([
                "sign",
                "--azure-key-vault-url", "https://test.vault.azure.net",
                "--azure-key-vault-certificate", "test-cert",
                "--azure-key-vault-client-id", "test-client-id",
                "--azure-credential-type", "ClientSecretCredential",
                "test.exe"
            ]);
        });

        // Assert
        Assert.AreNotEqual(0, ExitCode, "Expected non-zero exit code");
        Assert.IsTrue(StdErr.Contains("'--azure-key-vault-client-secret' is required when using ClientSecretCredential"),
            $"Expected client secret required error, got: {StdErr}");
    }

    [TestMethod]
    public async Task WhenClientSecretCredentialWithoutTenantId_ShouldShowError()
    {
        // Act
        (string StdOut, string StdErr, int ExitCode) = await CaptureOutput(async () =>
        {
            return await Program.Main([
                "sign",
                "--azure-key-vault-url", "https://test.vault.azure.net",
                "--azure-key-vault-certificate", "test-cert",
                "--azure-key-vault-client-id", "test-client-id",
                "--azure-key-vault-client-secret", "test-secret",
                "--azure-credential-type", "ClientSecretCredential",
                "test.exe"
            ]);
        });

        // Assert
        Assert.AreNotEqual(0, ExitCode, "Expected non-zero exit code");
        Assert.IsTrue(StdErr.Contains("'--azure-key-vault-tenant-id' is required when using ClientSecretCredential"),
            $"Expected tenant ID required error, got: {StdErr}");
    }

    [TestMethod]
    public async Task WhenAccessTokenCredentialWithoutToken_ShouldShowError()
    {
        // Act
        (string StdOut, string StdErr, int ExitCode) = await CaptureOutput(async () =>
        {
            return await Program.Main([
                "sign",
                "--azure-key-vault-url", "https://test.vault.azure.net",
                "--azure-key-vault-certificate", "test-cert",
                "--azure-credential-type", "AccessTokenCredential",
                "test.exe"
            ]);
        });

        // Assert
        Assert.AreNotEqual(0, ExitCode, "Expected non-zero exit code");
        Assert.IsTrue(StdErr.Contains("'--azure-key-vault-accesstoken' is required when using AccessTokenCredential"),
            $"Expected access token required error, got: {StdErr}");
    }

    [TestMethod]
    [DataRow("WorkloadIdentityCredential")]
    [DataRow("ManagedIdentityCredential")]
    [DataRow("EnvironmentCredential")]
    [DataRow("AzureCliCredential")]
    [DataRow("AzurePowerShellCredential")]
    [DataRow("DefaultAzureCredential")]
    [DataRow("InteractiveBrowserCredential")]
    public async Task WhenValidCredentialTypeWithoutExtraParams_ShouldPassValidation(string credentialType)
    {
        // Act
        (string StdOut, string StdErr, int ExitCode) = await CaptureOutput(async () =>
        {
            return await Program.Main([
                "sign",
                "--azure-key-vault-url", "https://test.vault.azure.net",
                "--azure-key-vault-certificate", "test-cert",
                "--azure-credential-type", credentialType,
                "nonexistent.exe" // Will fail at file existence check, but validation passes
            ]);
        });

        // Assert - Should fail on file not existing, not on validation
        Assert.AreNotEqual(0, ExitCode, "Expected non-zero exit code");
        Assert.IsTrue(StdErr.Contains("does not exist") || StdErr.Contains("At least one file must be specified"),
            $"Expected file not found error, got: {StdErr}");
        Assert.IsFalse(StdErr.Contains("required when using"),
            $"Should not have credential validation errors, got: {StdErr}");
    }

    [TestMethod]
    public async Task WhenManagedIdentityFlagUsedAlone_ShouldPassValidation()
    {
        // Act
        (string StdOut, string StdErr, int ExitCode) = await CaptureOutput(async () =>
        {
            return await Program.Main([
                "sign",
                "--azure-key-vault-url", "https://test.vault.azure.net",
                "--azure-key-vault-certificate", "test-cert",
                "--azure-key-vault-managed-identity",
                "nonexistent.exe"
            ]);
        });

        // Assert - Should fail on file not existing, not on validation
        Assert.AreNotEqual(0, ExitCode, "Expected non-zero exit code");
        Assert.IsTrue(StdErr.Contains("does not exist") || StdErr.Contains("At least one file must be specified"),
            $"Expected file not found error, got: {StdErr}");
    }

    private static async Task<(string StdOut, string StdErr, int Result)> CaptureOutput(Func<ValueTask<int>> act)
    {
        try
        {
            await _sync.WaitAsync();

            TextWriter oldStdOutWriter = Console.Out;
            TextWriter oldStdErrWriter = Console.Error;
            StringWriter stdOutWriter = new();
            StringWriter stdErrWriter = new();

            try
            {
                Console.SetOut(stdOutWriter);
                Console.SetError(stdErrWriter);
                int result = await act();
                return (stdOutWriter.ToString(), stdErrWriter.ToString(), result);
            }
            finally
            {
                Console.SetOut(oldStdOutWriter);
                Console.SetError(oldStdErrWriter);
            }
        }
        finally
        {
            _sync.Release();
        }
    }
}
