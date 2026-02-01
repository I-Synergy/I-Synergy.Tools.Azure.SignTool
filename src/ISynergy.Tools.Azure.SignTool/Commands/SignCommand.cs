using Azure.Security.KeyVault.Keys.Cryptography;
using ISynergy.Tools.Azure.SignTool.Configuration;
using ISynergy.Tools.Azure.SignTool.Core.Configurations;
using ISynergy.Tools.Azure.SignTool.Core.Enumerations;
using ISynergy.Tools.Azure.SignTool.Core.Services;
using ISynergy.Tools.Azure.SignTool.Credentials;
using ISynergy.Tools.Azure.SignTool.Enumerations;
using ISynergy.Tools.Azure.SignTool.Results;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.CommandLine;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ISynergy.Tools.Azure.SignTool.Commands;

internal static class SignCommand
{
    public static Command Create()
    {
        var command = new Command("sign", "Sign a file.");

        // Azure Key Vault options
        Option<string> keyVaultUrlOption = new("--azure-key-vault-url", "-kvu")
        {
            Description = "The URL to an Azure Key Vault."
        };

        Option<string> keyVaultClientIdOption = new("--azure-key-vault-client-id", "-kvi")
        {
            Description = "The Client ID to authenticate to the Azure Key Vault."
        };

        Option<string> keyVaultClientSecretOption = new("--azure-key-vault-client-secret", "-kvs")
        {
            Description = "The Client Secret to authenticate to the Azure Key Vault."
        };

        Option<string?> keyVaultTenantIdOption = new("--azure-key-vault-tenant-id", "-kvt")
        {
            Description = "The Tenant Id to authenticate to the Azure Key Vault."
        };

        Option<string?> keyVaultCertificateOption = new("--azure-key-vault-certificate", "-kvc")
        {
            Description = "The name of the certificate in Azure Key Vault."
        };

        Option<string?> keyVaultCertificateVersionOption = new("--azure-key-vault-certificate-version", "-kvcv")
        {
            Description = "The version of the certificate in Azure Key Vault to use."
        };

        Option<string?> keyVaultAccessTokenOption = new("--azure-key-vault-accesstoken", "-kva")
        {
            Description = "The Access Token to authenticate to the Azure Key Vault."
        };

        Option<bool> useManagedIdentityOption = new("--azure-key-vault-managed-identity", "-kvm")
        {
            Description = "Use the current Azure managed identity."
        };

        Option<string?> azureCredentialTypeOption = new("--azure-credential-type", "-act")
        {
            Description = "The type of Azure credential to use for authentication. Allowed values are: DefaultAzureCredential, ManagedIdentityCredential, WorkloadIdentityCredential, InteractiveBrowserCredential, EnvironmentCredential, AzureCliCredential, AzurePowerShellCredential, ClientSecretCredential, AccessTokenCredential."
        };

        Option<string?> azureAuthorityOption = new("--azure-authority", "-au")
        {
            Description = "The Azure Authority for Azure Key Vault."
        };

        // Signing options
        Option<string?> descriptionOption = new("--description", "-d")
        {
            Description = "Provide a description of the signed content."
        };

        Option<string?> descriptionUrlOption = new("--description-url", "-du")
        {
            Description = "Provide a URL with more information about the signed content."
        };

        Option<string?> rfc3161TimestampUrlOption = new("--timestamp-rfc3161", "-tr")
        {
            Description = "Specifies the RFC 3161 timestamp server's URL."
        };

        Option<string> timestampDigestOption = new("--timestamp-digest", "-td")
        {
            Description = "The digest algorithm used for timestamping.",
            DefaultValueFactory = _ => "SHA256"
        };

        Option<string> fileDigestOption = new("--file-digest", "-fd")
        {
            Description = "The digest algorithm to hash the file with.",
            DefaultValueFactory = _ => "SHA256"
        };

        Option<string?> authenticodeTimestampUrlOption = new("--timestamp-authenticode", "-t")
        {
            Description = "Specify the legacy timestamp server's URL."
        };

        Option<string[]> additionalCertificatesOption = new("--additional-certificates", "-ac")
        {
            Description = "Specify one or more certificates to include in the public certificate chain.",
            DefaultValueFactory = _ => Array.Empty<string>()
        };

        Option<bool> verboseOption = new("--verbose", "-v")
        {
            Description = "Include additional output in the log."
        };

        Option<bool> quietOption = new("--quiet", "-q")
        {
            Description = "Do not print any output to the console."
        };

        Option<bool> pageHashingOption = new("--page-hashing", "-ph")
        {
            Description = "Generate page hashes for executable files if supported."
        };

        Option<bool> noPageHashingOption = new("--no-page-hashing", "-nph")
        {
            Description = "Suppress page hashes for executable files if supported."
        };

        Option<bool> continueOnErrorOption = new("--continue-on-error", "-coe")
        {
            Description = "Continue signing multiple files if an error occurs."
        };

        Option<string?> inputFileListOption = new("--input-file-list", "-ifl")
        {
            Description = "A path to a file that contains a list of files, one per line, to sign."
        };

        Option<int> maxDegreeOfParallelismOption = new("--max-degree-of-parallelism", "-mdop")
        {
            Description = "The maximum number of concurrent signing operations.",
            DefaultValueFactory = _ => 4
        };

        Option<bool> colorsOption = new("--colors")
        {
            Description = "Enable color output on the command line."
        };

        Option<bool> skipSignedOption = new("--skip-signed", "-s")
        {
            Description = "Skip files that are already signed."
        };

        Option<bool> appendSignatureOption = new("--append-signature", "-as")
        {
            Description = "Append the signature, has no effect with --skip-signed."
        };

        // Files argument
        Argument<string[]> filesArgument = new("files")
        {
            Description = "The files to sign.",
            DefaultValueFactory = _ => Array.Empty<string>()
        };

        // Add all options to command
        command.Options.Add(keyVaultUrlOption);
        command.Options.Add(keyVaultClientIdOption);
        command.Options.Add(keyVaultClientSecretOption);
        command.Options.Add(keyVaultTenantIdOption);
        command.Options.Add(keyVaultCertificateOption);
        command.Options.Add(keyVaultCertificateVersionOption);
        command.Options.Add(keyVaultAccessTokenOption);
        command.Options.Add(useManagedIdentityOption);
        command.Options.Add(azureCredentialTypeOption);
        command.Options.Add(azureAuthorityOption);
        command.Options.Add(descriptionOption);
        command.Options.Add(descriptionUrlOption);
        command.Options.Add(rfc3161TimestampUrlOption);
        command.Options.Add(timestampDigestOption);
        command.Options.Add(fileDigestOption);
        command.Options.Add(authenticodeTimestampUrlOption);
        command.Options.Add(additionalCertificatesOption);
        command.Options.Add(verboseOption);
        command.Options.Add(quietOption);
        command.Options.Add(pageHashingOption);
        command.Options.Add(noPageHashingOption);
        command.Options.Add(continueOnErrorOption);
        command.Options.Add(inputFileListOption);
        command.Options.Add(maxDegreeOfParallelismOption);
        command.Options.Add(colorsOption);
        command.Options.Add(skipSignedOption);
        command.Options.Add(appendSignatureOption);
        command.Arguments.Add(filesArgument);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = new SignOptions
            {
                KeyVaultUrl = parseResult.GetValue(keyVaultUrlOption),
                KeyVaultClientId = parseResult.GetValue(keyVaultClientIdOption),
                KeyVaultClientSecret = parseResult.GetValue(keyVaultClientSecretOption),
                KeyVaultTenantId = parseResult.GetValue(keyVaultTenantIdOption),
                KeyVaultCertificate = parseResult.GetValue(keyVaultCertificateOption),
                KeyVaultCertificateVersion = parseResult.GetValue(keyVaultCertificateVersionOption),
                KeyVaultAccessToken = parseResult.GetValue(keyVaultAccessTokenOption),
                UseManagedIdentity = parseResult.GetValue(useManagedIdentityOption),
                AzureCredentialType = ParseAzureCredentialType(parseResult.GetValue(azureCredentialTypeOption)),
                AzureAuthority = parseResult.GetValue(azureAuthorityOption),
                SignDescription = parseResult.GetValue(descriptionOption),
                SignDescriptionUrl = parseResult.GetValue(descriptionUrlOption),
                Rfc3161TimestampUrl = parseResult.GetValue(rfc3161TimestampUrlOption),
                TimestampDigestAlgorithm = parseResult.GetValue(timestampDigestOption) ?? "SHA256",
                FileDigestAlgorithm = parseResult.GetValue(fileDigestOption) ?? "SHA256",
                AuthenticodeTimestampUrl = parseResult.GetValue(authenticodeTimestampUrlOption),
                AdditionalCertificates = parseResult.GetValue(additionalCertificatesOption) ?? [],
                Verbose = parseResult.GetValue(verboseOption),
                Quiet = parseResult.GetValue(quietOption),
                PageHashing = parseResult.GetValue(pageHashingOption),
                NoPageHashing = parseResult.GetValue(noPageHashingOption),
                ContinueOnError = parseResult.GetValue(continueOnErrorOption),
                InputFileList = parseResult.GetValue(inputFileListOption),
                MaxDegreeOfParallelism = parseResult.GetValue(maxDegreeOfParallelismOption),
                Colors = parseResult.GetValue(colorsOption),
                SkipSignedFiles = parseResult.GetValue(skipSignedOption),
                AppendSignature = parseResult.GetValue(appendSignatureOption),
                Files = parseResult.GetValue(filesArgument) ?? []
            };

            return await RunSignAsync(options);
        });

        return command;
    }

    private static async Task<int> RunSignAsync(SignOptions options)
    {
        if (!ValidateArguments(options))
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Use --help for additional information and usage.");
            return HRESULT.E_INVALIDARG;
        }

        using var loggerFactory = LoggerFactory.Create(builder => ConfigureLogging(builder, options));
        var logger = loggerFactory.CreateLogger("SignCommand");
        X509Certificate2Collection certificates;

        switch (GetAdditionalCertificates(options.AdditionalCertificates, logger))
        {
            case ErrorOr<X509Certificate2Collection>.Ok d:
                certificates = d.Value;
                break;
            case ErrorOr<X509Certificate2Collection>.Err err:
                logger.LogError(err.Error, "{Message}", err.Error.Message);
                return HRESULT.E_INVALIDARG;
            default:
                logger.LogError("Failed to include additional certificates.");
                return HRESULT.E_INVALIDARG;
        }

        var configuration = new AzureKeyVaultSignConfigurationSet
        {
            AzureKeyVaultUrl = new Uri(options.KeyVaultUrl!),
            AzureKeyVaultCertificateName = options.KeyVaultCertificate,
            AzureKeyVaultCertificateVersion = options.KeyVaultCertificateVersion,
            AzureClientId = options.KeyVaultClientId,
            AzureTenantId = options.KeyVaultTenantId,
            AzureAccessToken = options.KeyVaultAccessToken,
            AzureClientSecret = options.KeyVaultClientSecret,
            ManagedIdentity = options.UseManagedIdentity,
            AzureCredentialType = options.AzureCredentialType,
            AzureAuthority = options.AzureAuthority,
        };

        TimeStampConfiguration timeStampConfiguration;

        if (options.Rfc3161TimestampUrl is not null)
        {
            timeStampConfiguration = new TimeStampConfiguration(options.Rfc3161TimestampUrl, ParseHashAlgorithm(options.TimestampDigestAlgorithm), TimeStampType.RFC3161);
        }
        else if (options.AuthenticodeTimestampUrl is not null)
        {
            logger.LogWarning("Authenticode timestamps should only be used for compatibility purposes. RFC3161 timestamps should be used.");
            timeStampConfiguration = new TimeStampConfiguration(options.AuthenticodeTimestampUrl, default, TimeStampType.Authenticode);
        }
        else
        {
            logger.LogWarning("Signatures will not be timestamped. Signatures will become invalid when the signing certificate expires.");
            timeStampConfiguration = TimeStampConfiguration.None;
        }

        bool? performPageHashing = null;
        if (options.PageHashing)
        {
            performPageHashing = true;
        }
        if (options.NoPageHashing)
        {
            performPageHashing = false;
        }

        var configurationDiscoverer = new AzureKeyVaultConfigurationDiscoverer(logger);
        var materializedResult = await configurationDiscoverer.Materialize(configuration);
        AzureKeyVaultMaterializedConfiguration materialized;

        switch (materializedResult)
        {
            case ErrorOr<AzureKeyVaultMaterializedConfiguration>.Ok ok:
                materialized = ok.Value;
                break;
            default:
                logger.LogError("Failed to get configuration from Azure Key Vault.");
                return HRESULT.E_INVALIDARG;
        }

        const string RsaOid = "1.2.840.113549.1.1.1";
        if (materialized.PublicCertificate.GetKeyAlgorithm() is string alg and not RsaOid)
        {
            logger.LogError("Certificate algorithm is not RSA.");
            return HRESULT.E_INVALIDARG;
        }

        int failed = 0, succeeded = 0;
        var cancellationSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellationSource.Cancel();
            logger.LogInformation("Cancelling signing operations.");
        };

        var parallelOptions = new ParallelOptions();
        if (options.MaxDegreeOfParallelism != 0)
        {
            parallelOptions.MaxDegreeOfParallelism = options.MaxDegreeOfParallelism;
        }

        logger.LogTrace("Creating context");

        CryptographyClientOptions clientOptions = new()
        {
            Retry =
            {
                Delay = TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromSeconds(16),
                MaxRetries = 5,
                Mode = global::Azure.Core.RetryMode.Exponential
            }
        };

        var client = new CryptographyClient(materialized.KeyId, materialized.TokenCredential, clientOptions);
        var allFiles = GetAllFiles(options);

        using (var keyVault = await client.CreateRSAAsync())
        using (var signer = new CodeSigningService(keyVault, materialized.PublicCertificate, ParseHashAlgorithm(options.FileDigestAlgorithm), timeStampConfiguration, certificates))
        {
            var nugetSigner = new NuGetPackageSigner(signer, logger);

            Parallel.ForEach(allFiles, parallelOptions, () => (succeeded: 0, failed: 0), (filePath, pls, state) =>
            {
                if (cancellationSource.IsCancellationRequested)
                {
                    pls.Stop();
                }
                if (pls.IsStopped)
                {
                    return state;
                }
                using (logger.BeginScope("File: {Id}", filePath))
                {
                    logger.LogInformation("Signing file.");

                    if (options.SkipSignedFiles && IsSigned(filePath))
                    {
                        logger.LogInformation("Skipping already signed file.");
                        return (state.succeeded + 1, state.failed);
                    }

                    int result;

                    // Check if this is a NuGet package
                    if (NuGetPackageSigner.IsNuGetPackage(filePath))
                    {
                        result = nugetSigner.SignPackage(filePath, options.SignDescription, options.SignDescriptionUrl, performPageHashing, options.AppendSignature);
                    }
                    else
                    {
                        result = signer.SignFile(filePath, options.SignDescription, options.SignDescriptionUrl, performPageHashing, logger, options.AppendSignature);
                    }

                    switch (result)
                    {
                        case HRESULT.COR_E_BADIMAGEFORMAT:
                            logger.LogError("The Publisher Identity in the AppxManifest.xml does not match the subject on the certificate.");
                            break;
                        case HRESULT.TRUST_E_SUBJECT_FORM_UNKNOWN:
                            logger.LogError("The file cannot be signed because it is not a recognized file type for signing or it is corrupt.");
                            break;
                    }

                    if (result == HRESULT.S_OK)
                    {
                        logger.LogInformation("Signing completed successfully.");
                        return (state.succeeded + 1, state.failed);
                    }
                    else
                    {
                        logger.LogError("Signing failed with error {result}.", $"{result:X2}");
                        if (!options.ContinueOnError || allFiles.Count == 1)
                        {
                            logger.LogInformation("Stopping file signing.");
                            pls.Stop();
                        }

                        return (state.succeeded, state.failed + 1);
                    }
                }
            }, result =>
            {
                Interlocked.Add(ref failed, result.failed);
                Interlocked.Add(ref succeeded, result.succeeded);
            });
        }

        logger.LogInformation("Successful operations: {succeeded}", succeeded);
        logger.LogInformation("Failed operations: {failed}", failed);

        if (failed > 0 && succeeded == 0)
        {
            return HRESULT.E_ALL_FAILED;
        }
        else if (failed > 0)
        {
            return HRESULT.S_SOME_SUCCESS;
        }
        else
        {
            return HRESULT.S_OK;
        }
    }

    private static HashSet<string> GetAllFiles(SignOptions options)
    {
        var allFiles = new HashSet<string>();

        foreach (string file in options.Files)
        {
            AddFile(allFiles, file);
        }

        if (!string.IsNullOrWhiteSpace(options.InputFileList))
        {
            foreach (string line in File.ReadLines(options.InputFileList))
            {
                AddFile(allFiles, line);
            }
        }

        // Check for corresponding .snupkg files for each .nupkg
        var nupkgFiles = allFiles.Where(f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var nupkg in nupkgFiles)
        {
            var snupkg = Path.ChangeExtension(nupkg, ".snupkg");
            if (File.Exists(snupkg) && !allFiles.Contains(snupkg))
            {
                allFiles.Add(snupkg);
            }
        }

        return allFiles;

        static void AddFile(HashSet<string> collection, string item)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                return;
            }

            if (item.Contains('*'))
            {
                Matcher matcher = new();
                string directory;

                if (Path.IsPathFullyQualified(item) && Path.GetPathRoot(item) is string root)
                {
                    directory = root;
                    matcher.AddInclude(Path.GetRelativePath(root, item));
                }
                else
                {
                    directory = ".";
                    matcher.AddInclude(item);
                }

                foreach (string match in matcher.GetResultsInFullPath(directory))
                {
                    collection.Add(match);
                }
            }
            else
            {
                collection.Add(item);
            }
        }
    }

    private static bool IsSigned(string filePath)
    {
        try
        {
            return X509Certificate2.GetCertContentType(filePath) == X509ContentType.Authenticode;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static bool ValidateArguments(SignOptions options)
    {
        bool valid = true;

        if (options.KeyVaultUrl is null)
        {
            Console.Error.WriteLine("--azure-key-vault-url is required.");
            valid = false;
        }

        if (options.KeyVaultCertificate is null)
        {
            Console.Error.WriteLine("--azure-key-vault-certificate is required.");
            valid = false;
        }

        if (options.PageHashing && options.NoPageHashing)
        {
            Console.Error.WriteLine("Cannot use '--page-hashing' and '--no-page-hashing' options together.");
            valid = false;
        }

        if (options.Quiet && options.Verbose)
        {
            Console.Error.WriteLine("Cannot use '--quiet' and '--verbose' options together.");
            valid = false;
        }

        if (!OneTrue(options.KeyVaultAccessToken is not null, options.KeyVaultClientId is not null, options.UseManagedIdentity, options.AzureCredentialType.HasValue))
        {
            Console.Error.WriteLine("One of '--azure-key-vault-accesstoken', '--azure-key-vault-client-id', '--azure-key-vault-managed-identity' or '--azure-credential-type' must be supplied.");
            valid = false;
        }

        if (options.Rfc3161TimestampUrl is not null && options.AuthenticodeTimestampUrl is not null)
        {
            Console.Error.WriteLine("Cannot use '--timestamp-rfc3161' and '--timestamp-authenticode' options together.");
            valid = false;
        }

        if (options.KeyVaultClientId is not null && options.KeyVaultClientSecret is null)
        {
            Console.Error.WriteLine("Must supply '--azure-key-vault-client-secret' when using '--azure-key-vault-client-id'.");
            valid = false;
        }

        if (options.KeyVaultClientId is not null && options.KeyVaultTenantId is null)
        {
            Console.Error.WriteLine("Must supply '--azure-key-vault-tenant-id' when using '--azure-key-vault-client-id'.");
            valid = false;
        }

        if (options.UseManagedIdentity && (options.KeyVaultAccessToken is not null || options.KeyVaultClientId is not null || options.AzureCredentialType.HasValue))
        {
            Console.Error.WriteLine("Cannot use '--azure-key-vault-managed-identity' with '--azure-key-vault-accesstoken', '--azure-key-vault-client-id', or '--azure-credential-type'.");
            valid = false;
        }

        if (options.AzureCredentialType.HasValue && (options.KeyVaultAccessToken is not null || options.KeyVaultClientId is not null || options.UseManagedIdentity))
        {
            Console.Error.WriteLine("Cannot use '--azure-credential-type' with '--azure-key-vault-accesstoken', '--azure-key-vault-client-id', or '--azure-key-vault-managed-identity'.");
            valid = false;
        }

        // Validate the credential type value if provided
        if (options.AzureCredentialType.HasValue)
        {
            // Validate that ClientSecretCredential has required parameters
            if (options.AzureCredentialType == Enumerations.AzureCredentialType.ClientSecretCredential)
            {
                if (string.IsNullOrWhiteSpace(options.KeyVaultClientId))
                {
                    Console.Error.WriteLine("'--azure-key-vault-client-id' is required when using ClientSecretCredential.");
                    valid = false;
                }
                if (string.IsNullOrWhiteSpace(options.KeyVaultClientSecret))
                {
                    Console.Error.WriteLine("'--azure-key-vault-client-secret' is required when using ClientSecretCredential.");
                    valid = false;
                }
                if (string.IsNullOrWhiteSpace(options.KeyVaultTenantId))
                {
                    Console.Error.WriteLine("'--azure-key-vault-tenant-id' is required when using ClientSecretCredential.");
                    valid = false;
                }
            }

            // Validate that AccessTokenCredential has required parameters
            if (options.AzureCredentialType == Enumerations.AzureCredentialType.AccessTokenCredential && string.IsNullOrWhiteSpace(options.KeyVaultAccessToken))
            {
                Console.Error.WriteLine("'--azure-key-vault-accesstoken' is required when using AccessTokenCredential.");
                valid = false;
            }
        }

        if (options.AppendSignature && !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348))
        {
            Console.Error.WriteLine("'--append-signature' requires Windows Server 2022, Windows 11 or later.");
            valid = false;
        }

        if (options.AppendSignature && options.AuthenticodeTimestampUrl is not null)
        {
            Console.Error.WriteLine("Cannot use '--append-signature' and '--timestamp-authenticode' options together.");
            valid = false;
        }

        if (options.InputFileList is not null && !File.Exists(options.InputFileList))
        {
            Console.Error.WriteLine($"File '{options.InputFileList}' does not exist.");
            valid = false;
        }

        valid &= ValidateHashAlgorithm(options.FileDigestAlgorithm, "--file-digest");
        valid &= ValidateHashAlgorithm(options.TimestampDigestAlgorithm, "--timestamp-digest");

        if (options.MaxDegreeOfParallelism < -1)
        {
            Console.Error.WriteLine("'--max-degree-of-parallelism' must be a positive integer, zero, or -1.");
            valid = false;
        }

        if (options.AzureAuthority is not null && AuthorityHostNames.GetUriForAzureAuthorityIdentifier(options.AzureAuthority) is null)
        {
            Console.Error.WriteLine($"'{options.AzureAuthority}' is not a valid value for '--azure-authority'. Allowed values are [{string.Join(", ", AuthorityHostNames.Keys)}].");
            valid = false;
        }

        var allFiles = GetAllFiles(options);
        if (allFiles.Count == 0)
        {
            Console.Error.WriteLine("At least one file must be specified to sign.");
            valid = false;
        }
        else
        {
            foreach (string file in allFiles)
            {
                if (!File.Exists(file))
                {
                    Console.Error.WriteLine($"File '{file}' does not exist.");
                    valid = false;
                }
            }
        }

        return valid;
    }

    private static bool ValidateHashAlgorithm(string? input, string optionName)
    {
        if (input is null)
        {
            Console.Error.WriteLine($"'{optionName}' is required. Allowed values are [{string.Join(", ", s_hashAlgorithm)}].");
            return false;
        }

        foreach (string a in s_hashAlgorithm)
        {
            if (input.Equals(a, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        Console.Error.WriteLine($"'{input}' is not a valid hash algorithm for '{optionName}'. Allowed values are [{string.Join(", ", s_hashAlgorithm)}].");
        return false;
    }

    private static void ConfigureLogging(ILoggingBuilder builder, SignOptions options)
    {
        builder.AddSimpleConsole(console =>
        {
            console.IncludeScopes = true;
            console.ColorBehavior = options.Colors ? LoggerColorBehavior.Enabled : LoggerColorBehavior.Disabled;
        });

        builder.SetMinimumLevel(GetLogLevel(options));
    }

    private static LogLevel GetLogLevel(SignOptions options)
    {
        if (options.Quiet)
        {
            return LogLevel.Critical;
        }
        else if (options.Verbose)
        {
            return LogLevel.Trace;
        }
        else
        {
            return LogLevel.Information;
        }
    }

    private static ErrorOr<X509Certificate2Collection> GetAdditionalCertificates(string[] paths, ILogger logger)
    {
        var collection = new X509Certificate2Collection();
        try
        {
            foreach (var path in paths)
            {
                var type = X509Certificate2.GetCertContentType(path);
                switch (type)
                {
                    case X509ContentType.Cert:
                    case X509ContentType.Authenticode:
                    case X509ContentType.SerializedCert:
                        var certificate = X509CertificateLoader.LoadCertificateFromFile(path);
                        logger.LogTrace("Including additional certificate {thumbprint}.", certificate.Thumbprint);
                        collection.Add(certificate);
                        break;
                    default:
                        return new Exception($"Specified file {path} is not a public valid certificate.");
                }
            }
        }
        catch (CryptographicException e)
        {
            logger.LogError(e, "An exception occurred while including an additional certificate.");
            return e;
        }

        return collection;
    }

    private static HashAlgorithmName ParseHashAlgorithm(string? hashAlgorithm)
    {
        if ("SHA1".Equals(hashAlgorithm, StringComparison.OrdinalIgnoreCase))
        {
            return HashAlgorithmName.SHA1;
        }
        if ("SHA256".Equals(hashAlgorithm, StringComparison.OrdinalIgnoreCase))
        {
            return HashAlgorithmName.SHA256;
        }
        if ("SHA384".Equals(hashAlgorithm, StringComparison.OrdinalIgnoreCase))
        {
            return HashAlgorithmName.SHA384;
        }
        if ("SHA512".Equals(hashAlgorithm, StringComparison.OrdinalIgnoreCase))
        {
            return HashAlgorithmName.SHA512;
        }

        throw new ArgumentException("Invalid hash algorithm", nameof(hashAlgorithm));
    }

    private static bool OneTrue(params bool[] values)
    {
        int count = 0;

        for (int i = 0; i < values.Length && count < 2; i++)
        {
            if (values[i])
            {
                count++;
            }
        }

        return count == 1;
    }

    private static AzureCredentialType? ParseAzureCredentialType(string? credentialType)
    {
        if (string.IsNullOrWhiteSpace(credentialType))
        {
            return null;
        }

        if (Enum.TryParse<AzureCredentialType>(credentialType, ignoreCase: true, out var result))
        {
            return result;
        }

        return null;
    }

    private static readonly string[] s_hashAlgorithm = ["SHA1", "SHA256", "SHA384", "SHA512"];
}

internal sealed class SignOptions
{
    public string? KeyVaultUrl { get; set; }
    public string? KeyVaultClientId { get; set; }
    public string? KeyVaultClientSecret { get; set; }
    public string? KeyVaultTenantId { get; set; }
    public string? KeyVaultCertificate { get; set; }
    public string? KeyVaultCertificateVersion { get; set; }
    public string? KeyVaultAccessToken { get; set; }
    public bool UseManagedIdentity { get; set; }
    public AzureCredentialType? AzureCredentialType { get; set; }
    public string? AzureAuthority { get; set; }
    public string? SignDescription { get; set; }
    public string? SignDescriptionUrl { get; set; }
    public string? Rfc3161TimestampUrl { get; set; }
    public string TimestampDigestAlgorithm { get; set; } = "SHA256";
    public string FileDigestAlgorithm { get; set; } = "SHA256";
    public string? AuthenticodeTimestampUrl { get; set; }
    public string[] AdditionalCertificates { get; set; } = [];
    public bool Verbose { get; set; }
    public bool Quiet { get; set; }
    public bool PageHashing { get; set; }
    public bool NoPageHashing { get; set; }
    public bool ContinueOnError { get; set; }
    public string? InputFileList { get; set; }
    public int MaxDegreeOfParallelism { get; set; } = 4;
    public bool Colors { get; set; }
    public bool SkipSignedFiles { get; set; }
    public bool AppendSignature { get; set; }
    public string[] Files { get; set; } = [];
}
