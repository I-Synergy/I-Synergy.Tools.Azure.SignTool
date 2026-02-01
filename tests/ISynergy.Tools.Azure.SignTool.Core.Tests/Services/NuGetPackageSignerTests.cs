using ISynergy.Tools.Azure.SignTool.Core.Abstractions.Services;
using ISynergy.Tools.Azure.SignTool.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO.Compression;
using System.Security.Cryptography;

namespace ISynergy.Tools.Azure.SignTool.Core.Tests.Services;

[TestClass]
public class NuGetPackageSignerTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"nuget_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [TestMethod]
    public void IsNuGetPackageShouldReturnTrueForNupkg()
    {
        Assert.IsTrue(NuGetPackageSigner.IsNuGetPackage("test.nupkg"));
        Assert.IsTrue(NuGetPackageSigner.IsNuGetPackage("test.NUPKG"));
        Assert.IsTrue(NuGetPackageSigner.IsNuGetPackage("C:\\path\\to\\test.nupkg"));
    }

    [TestMethod]
    public void IsNuGetPackageShouldReturnTrueForSnupkg()
    {
        Assert.IsTrue(NuGetPackageSigner.IsNuGetPackage("test.snupkg"));
        Assert.IsTrue(NuGetPackageSigner.IsNuGetPackage("test.SNUPKG"));
        Assert.IsTrue(NuGetPackageSigner.IsNuGetPackage("C:\\path\\to\\test.snupkg"));
    }

    [TestMethod]
    public void IsNuGetPackageShouldReturnFalseForOtherExtensions()
    {
        Assert.IsFalse(NuGetPackageSigner.IsNuGetPackage("test.exe"));
        Assert.IsFalse(NuGetPackageSigner.IsNuGetPackage("test.dll"));
        Assert.IsFalse(NuGetPackageSigner.IsNuGetPackage("test.zip"));
        Assert.IsFalse(NuGetPackageSigner.IsNuGetPackage("test.txt"));
        Assert.IsFalse(NuGetPackageSigner.IsNuGetPackage("test"));
    }

    [TestMethod]
    public void IsNuGetPackageShouldBeCaseInsensitive()
    {
        Assert.IsTrue(NuGetPackageSigner.IsNuGetPackage("TeSt.NuPkG"));
        Assert.IsTrue(NuGetPackageSigner.IsNuGetPackage("TeSt.SnUpKg"));
    }

    [TestMethod]
    public void IsNuGetPackageShouldHandlePathsWithDots()
    {
        Assert.IsTrue(NuGetPackageSigner.IsNuGetPackage("My.Package.1.0.0.nupkg"));
        Assert.IsTrue(NuGetPackageSigner.IsNuGetPackage("My.Package.1.0.0.snupkg"));
    }

    [TestMethod]
    public void NuGetPackageStructureTest()
    {
        // This test verifies the package structure is preserved after extraction/repackaging
        var packagePath = CreateTestNuGetPackage("StructureTest.nupkg", true, true);
        
        // Extract and verify structure
        var extractDir = Path.Combine(_testDirectory, "extract");
        ZipFile.ExtractToDirectory(packagePath, extractDir);

        // Verify expected files exist
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "StructureTest.nuspec")));
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "lib", "net10.0", "TestLibrary.dll")));
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "readme.txt")));

        // Verify DLL is in the lib folder
        var dllPath = Path.Combine(extractDir, "lib", "net10.0", "TestLibrary.dll");
        Assert.IsTrue(File.Exists(dllPath));
    }

    [TestMethod]
    public void NuGetPackageWithMultipleDllsStructureTest()
    {
        var packagePath = CreateTestNuGetPackageWithMultipleDlls("MultiDll.nupkg");
        
        var extractDir = Path.Combine(_testDirectory, "multi_extract");
        ZipFile.ExtractToDirectory(packagePath, extractDir);

        // Verify all DLLs exist
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "lib", "net10.0", "Library1.dll")));
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "lib", "net10.0", "Library2.dll")));
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "tools", "Tool.exe")));
    }

    [TestMethod]
    public void NuGetPackageEmptyPackageStructureTest()
    {
        var packagePath = CreateTestNuGetPackage("Empty.nupkg", false, false);

        var extractDir = Path.Combine(_testDirectory, "empty_extract");
        ZipFile.ExtractToDirectory(packagePath, extractDir);

        // Verify nuspec exists but no DLLs
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "Empty.nuspec")));
        var files = Directory.GetFiles(extractDir, "*.dll", SearchOption.AllDirectories);
        Assert.AreEqual(0, files.Length);
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithCodeSigningServiceOnly_DoesNotThrow()
    {
        var mockCodeSigner = new MockCodeSigningService();
        var signer = new NuGetPackageSigner(mockCodeSigner);
        Assert.IsNotNull(signer);
    }

    [TestMethod]
    public void Constructor_WithNullCodeSigningService_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new NuGetPackageSigner(null!));
    }

    [TestMethod]
    public void Constructor_WithFullParameters_DoesNotThrow()
    {
        var mockCodeSigner = new MockCodeSigningService();
        var signer = new NuGetPackageSigner(
            mockCodeSigner,
            null, // nugetSignatureService can be null
            "http://timestamp.example.com",
            HashAlgorithmName.SHA256,
            null);
        Assert.IsNotNull(signer);
    }

    #endregion

    #region SignPackageAsync Tests

    [TestMethod]
    public async Task SignPackageAsync_WithNonExistentPackage_ReturnsFileNotFoundError()
    {
        var mockCodeSigner = new MockCodeSigningService();
        var signer = new NuGetPackageSigner(mockCodeSigner);

        var result = await signer.SignPackageAsync(
            Path.Combine(_testDirectory, "nonexistent.nupkg"),
            null,
            null,
            null);

        // ERROR_FILE_NOT_FOUND = 0x80070002
        Assert.AreEqual(unchecked((int)0x80070002), result);
    }

    [TestMethod]
    public async Task SignPackageAsync_WithValidPackage_ExtractsAndRepackages()
    {
        var packagePath = CreateTestNuGetPackage("AsyncTest.nupkg", true, true);
        var mockCodeSigner = new MockCodeSigningService();
        var signer = new NuGetPackageSigner(mockCodeSigner);

        var originalContent = File.ReadAllBytes(packagePath);
        // signContents = true to test extraction and repackaging
        var result = await signer.SignPackageAsync(packagePath, null, null, null, false, true);

        // Should succeed (mock signer always returns 0)
        Assert.AreEqual(0, result);

        // Package should still exist
        Assert.IsTrue(File.Exists(packagePath));

        // Verify structure is preserved
        var extractDir = Path.Combine(_testDirectory, "async_extract");
        ZipFile.ExtractToDirectory(packagePath, extractDir);
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "AsyncTest.nuspec")));
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "lib", "net10.0", "TestLibrary.dll")));
    }

    [TestMethod]
    public async Task SignPackageAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        var packagePath = CreateTestNuGetPackage("CancelTest.nupkg", true, false);
        var mockCodeSigner = new MockCodeSigningService { DelayMs = 1000 };
        var signer = new NuGetPackageSigner(mockCodeSigner);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            await signer.SignPackageAsync(packagePath, null, null, null, false, true, null, cts.Token));
    }

    [TestMethod]
    public async Task SignPackageAsync_WithEmptyPackage_ReturnsSuccess()
    {
        var packagePath = CreateTestNuGetPackage("EmptyAsync.nupkg", false, false);
        var mockCodeSigner = new MockCodeSigningService();
        var signer = new NuGetPackageSigner(mockCodeSigner);

        // signContents = true but no files to sign
        var result = await signer.SignPackageAsync(packagePath, null, null, null, false, true);

        Assert.AreEqual(0, result);
        Assert.AreEqual(0, mockCodeSigner.SignFileCallCount); // No files to sign
    }

    [TestMethod]
    public async Task SignPackageAsync_WithMultipleDlls_SignsAllFiles()
    {
        var packagePath = CreateTestNuGetPackageWithMultipleDlls("MultiAsync.nupkg");
        var mockCodeSigner = new MockCodeSigningService();
        var signer = new NuGetPackageSigner(mockCodeSigner);

        // signContents = true to sign all files
        var result = await signer.SignPackageAsync(packagePath, null, null, null, false, true);

        Assert.AreEqual(0, result);
        // Should have signed Library1.dll, Library2.dll, and Tool.exe
        Assert.AreEqual(3, mockCodeSigner.SignFileCallCount);
    }

    [TestMethod]
    public async Task SignPackageAsync_WhenSigningFails_ReturnsErrorCode()
    {
        var packagePath = CreateTestNuGetPackage("FailTest.nupkg", true, false);
        var mockCodeSigner = new MockCodeSigningService { ReturnCode = unchecked((int)0x80004005) };
        var signer = new NuGetPackageSigner(mockCodeSigner);

        // signContents = true to trigger content signing failure
        var result = await signer.SignPackageAsync(packagePath, null, null, null, false, true);

        Assert.AreEqual(unchecked((int)0x80004005), result);
    }

    [TestMethod]
    public async Task SignPackageAsync_SkipsNuGetSignatureForSnupkg()
    {
        var packagePath = CreateTestNuGetPackage("Symbol.snupkg", true, false);
        // Rename to snupkg
        var snupkgPath = Path.ChangeExtension(packagePath, ".snupkg");
        File.Move(packagePath, snupkgPath);

        var mockCodeSigner = new MockCodeSigningService();
        var mockNugetSigner = new MockNuGetPackageSignatureService();
        var signer = new NuGetPackageSigner(
            mockCodeSigner,
            mockNugetSigner,
            null,
            HashAlgorithmName.SHA256);

        // signContents = true to test content signing for snupkg
        var result = await signer.SignPackageAsync(snupkgPath, null, null, null, false, true);

        Assert.AreEqual(0, result);
        // NuGet signature service should NOT be called for .snupkg files
        Assert.AreEqual(0, mockNugetSigner.SignCallCount);
    }

    [TestMethod]
    public async Task SignPackageAsync_CallsNuGetSignatureServiceForNupkg()
    {
        var packagePath = CreateTestNuGetPackage("NuGetSign.nupkg", true, false);
        var mockCodeSigner = new MockCodeSigningService();
        var mockNugetSigner = new MockNuGetPackageSignatureService();
        var signer = new NuGetPackageSigner(
            mockCodeSigner,
            mockNugetSigner,
            "http://timestamp.example.com",
            HashAlgorithmName.SHA256);

        // signContents = false (default), only package signature
        var result = await signer.SignPackageAsync(packagePath, null, null, null, false, false);

        Assert.AreEqual(0, result);
        // NuGet signature service should be called for .nupkg files
        Assert.AreEqual(1, mockNugetSigner.SignCallCount);
        // Code signer should NOT be called (no content signing)
        Assert.AreEqual(0, mockCodeSigner.SignFileCallCount);
    }

    [TestMethod]
    public async Task SignPackageAsync_ByDefault_DoesNotSignContents()
    {
        var packagePath = CreateTestNuGetPackage("NoContentSign.nupkg", true, false);
        var mockCodeSigner = new MockCodeSigningService();
        var mockNugetSigner = new MockNuGetPackageSignatureService();
        var signer = new NuGetPackageSigner(
            mockCodeSigner,
            mockNugetSigner,
            "http://timestamp.example.com",
            HashAlgorithmName.SHA256);

        // Default behavior - signContents = false
        var result = await signer.SignPackageAsync(packagePath, null, null, null, false, false);

        Assert.AreEqual(0, result);
        // Code signer should NOT be called (no content signing)
        Assert.AreEqual(0, mockCodeSigner.SignFileCallCount);
        // NuGet signature service should still be called
        Assert.AreEqual(1, mockNugetSigner.SignCallCount);
    }

    [TestMethod]
    public async Task SignPackageAsync_WithSignContents_SignsBinaries()
    {
        var packagePath = CreateTestNuGetPackage("ContentSign.nupkg", true, false);
        var mockCodeSigner = new MockCodeSigningService();
        var mockNugetSigner = new MockNuGetPackageSignatureService();
        var signer = new NuGetPackageSigner(
            mockCodeSigner,
            mockNugetSigner,
            "http://timestamp.example.com",
            HashAlgorithmName.SHA256);

        // signContents = true
        var result = await signer.SignPackageAsync(packagePath, null, null, null, false, true);

        Assert.AreEqual(0, result);
        // Code signer should be called for the DLL inside
        Assert.AreEqual(1, mockCodeSigner.SignFileCallCount);
        // NuGet signature service should also be called
        Assert.AreEqual(1, mockNugetSigner.SignCallCount);
    }

    [TestMethod]
    public async Task SignPackageAsync_WithSignContentsAndMultipleDlls_SignsAllBinaries()
    {
        var packagePath = CreateTestNuGetPackageWithMultipleDlls("MultiContentSign.nupkg");
        var mockCodeSigner = new MockCodeSigningService();
        var mockNugetSigner = new MockNuGetPackageSignatureService();
        var signer = new NuGetPackageSigner(
            mockCodeSigner,
            mockNugetSigner,
            "http://timestamp.example.com",
            HashAlgorithmName.SHA256);

        // signContents = true
        var result = await signer.SignPackageAsync(packagePath, null, null, null, false, true);

        Assert.AreEqual(0, result);
        // Should have signed Library1.dll, Library2.dll, and Tool.exe
        Assert.AreEqual(3, mockCodeSigner.SignFileCallCount);
        // NuGet signature service should also be called
        Assert.AreEqual(1, mockNugetSigner.SignCallCount);
    }

    [TestMethod]
    public async Task SignPackageAsync_WithoutSignContents_SnupkgNotSigned()
    {
        var packagePath = CreateTestNuGetPackage("SymbolNoSign.snupkg", true, false);
        // Rename to snupkg
        var snupkgPath = Path.ChangeExtension(packagePath, ".snupkg");
        File.Move(packagePath, snupkgPath);

        var mockCodeSigner = new MockCodeSigningService();
        var mockNugetSigner = new MockNuGetPackageSignatureService();
        var signer = new NuGetPackageSigner(
            mockCodeSigner,
            mockNugetSigner,
            "http://timestamp.example.com",
            HashAlgorithmName.SHA256);

        // signContents = false (default)
        var result = await signer.SignPackageAsync(snupkgPath, null, null, null, false, false);

        Assert.AreEqual(0, result);
        // No content signing
        Assert.AreEqual(0, mockCodeSigner.SignFileCallCount);
        // No NuGet signature for .snupkg
        Assert.AreEqual(0, mockNugetSigner.SignCallCount);
    }

    [TestMethod]
    public async Task SignPackageAsync_WithFilter_OnlySignsMatchingFiles()
    {
        var packagePath = CreateTestNuGetPackageWithMixedDlls("FilterTest.nupkg");
        var mockCodeSigner = new MockCodeSigningService();
        var mockNugetSigner = new MockNuGetPackageSignatureService();
        var signer = new NuGetPackageSigner(
            mockCodeSigner,
            mockNugetSigner,
            "http://timestamp.example.com",
            HashAlgorithmName.SHA256);

        // signContents = true with filter for ISynergy.* files
        var result = await signer.SignPackageAsync(
            packagePath, null, null, null, false, true, "ISynergy.*");

        Assert.AreEqual(0, result);
        // Only ISynergy.Framework.dll and ISynergy.Core.dll should be signed (2 files)
        Assert.AreEqual(2, mockCodeSigner.SignFileCallCount);
        // NuGet signature should still be called
        Assert.AreEqual(1, mockNugetSigner.SignCallCount);
    }

    [TestMethod]
    public async Task SignPackageAsync_WithFilter_NoMatchingFiles_SignsNothing()
    {
        var packagePath = CreateTestNuGetPackageWithMixedDlls("NoMatchFilter.nupkg");
        var mockCodeSigner = new MockCodeSigningService();
        var mockNugetSigner = new MockNuGetPackageSignatureService();
        var signer = new NuGetPackageSigner(
            mockCodeSigner,
            mockNugetSigner,
            "http://timestamp.example.com",
            HashAlgorithmName.SHA256);

        // signContents = true with filter that matches nothing
        var result = await signer.SignPackageAsync(
            packagePath, null, null, null, false, true, "NonExistent.*");

        Assert.AreEqual(0, result);
        // No files should be signed
        Assert.AreEqual(0, mockCodeSigner.SignFileCallCount);
        // NuGet signature should still be called
        Assert.AreEqual(1, mockNugetSigner.SignCallCount);
    }

    [TestMethod]
    public async Task SignPackageAsync_WithoutFilter_SignsAllFiles()
    {
        var packagePath = CreateTestNuGetPackageWithMixedDlls("AllFiles.nupkg");
        var mockCodeSigner = new MockCodeSigningService();
        var mockNugetSigner = new MockNuGetPackageSignatureService();
        var signer = new NuGetPackageSigner(
            mockCodeSigner,
            mockNugetSigner,
            "http://timestamp.example.com",
            HashAlgorithmName.SHA256);

        // signContents = true without filter
        var result = await signer.SignPackageAsync(
            packagePath, null, null, null, false, true, null);

        Assert.AreEqual(0, result);
        // All 4 files should be signed (ISynergy.Framework.dll, ISynergy.Core.dll, Newtonsoft.Json.dll, ThirdParty.dll)
        Assert.AreEqual(4, mockCodeSigner.SignFileCallCount);
        // NuGet signature should be called
        Assert.AreEqual(1, mockNugetSigner.SignCallCount);
    }

    [TestMethod]
    public async Task SignPackageAsync_WithWildcardFilter_MatchesCorrectly()
    {
        var packagePath = CreateTestNuGetPackageWithMixedDlls("WildcardFilter.nupkg");
        var mockCodeSigner = new MockCodeSigningService();
        var signer = new NuGetPackageSigner(mockCodeSigner);

        // signContents = true with wildcard filter for all .dll files containing "Json"
        var result = await signer.SignPackageAsync(
            packagePath, null, null, null, false, true, "*Json*");

        Assert.AreEqual(0, result);
        // Only Newtonsoft.Json.dll should be signed
        Assert.AreEqual(1, mockCodeSigner.SignFileCallCount);
    }

    #endregion

    #region Mock Classes

    private class MockCodeSigningService : ICodeSigningService
    {
        public int ReturnCode { get; set; } = 0;
        public int SignFileCallCount { get; private set; }
        public int DelayMs { get; set; } = 0;

        public int SignFile(
            ReadOnlySpan<char> path,
            ReadOnlySpan<char> description,
            ReadOnlySpan<char> descriptionUrl,
            bool? pageHashing,
            Microsoft.Extensions.Logging.ILogger? logger = null,
            bool appendSignature = false)
        {
            if (DelayMs > 0)
            {
                Thread.Sleep(DelayMs);
            }
            SignFileCallCount++;
            return ReturnCode;
        }

        public void Dispose() { }
    }

    private class MockNuGetPackageSignatureService : INuGetPackageSignatureService
    {
        public int SignCallCount { get; private set; }
        public int ReturnCode { get; set; } = 0;

        public Task<int> SignPackageAsync(
            string packagePath,
            string outputPath,
            string? timestampUrl,
            HashAlgorithmName hashAlgorithm,
            CancellationToken cancellationToken = default)
        {
            SignCallCount++;
            return Task.FromResult(ReturnCode);
        }

        public void Dispose() { }
    }

    #endregion

    private string CreateTestNuGetPackage(string fileName, bool includeDll = false, bool includeNonExecutable = false)
    {
        var packagePath = Path.Combine(_testDirectory, fileName);
        var tempExtractDir = Path.Combine(_testDirectory, $"temp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempExtractDir);

        try
        {
            // Create a basic .nuspec file
            var packageId = Path.GetFileNameWithoutExtension(fileName);
            var nuspecContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>{packageId}</id>
    <version>1.0.0</version>
    <authors>Test</authors>
    <description>Test package</description>
  </metadata>
</package>";
            File.WriteAllText(Path.Combine(tempExtractDir, $"{packageId}.nuspec"), nuspecContent);

            if (includeDll)
            {
                var libDir = Path.Combine(tempExtractDir, "lib", "net10.0");
                Directory.CreateDirectory(libDir);
                File.WriteAllText(Path.Combine(libDir, "TestLibrary.dll"), "fake dll content");
            }

            if (includeNonExecutable)
            {
                File.WriteAllText(Path.Combine(tempExtractDir, "readme.txt"), "readme content");
            }

            // Create the zip file
            ZipFile.CreateFromDirectory(tempExtractDir, packagePath, CompressionLevel.Optimal, false);

            return packagePath;
        }
        finally
        {
            if (Directory.Exists(tempExtractDir))
            {
                Directory.Delete(tempExtractDir, true);
            }
        }
    }

    private string CreateTestNuGetPackageWithMultipleDlls(string fileName)
    {
        var packagePath = Path.Combine(_testDirectory, fileName);
        var tempExtractDir = Path.Combine(_testDirectory, $"temp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempExtractDir);

        try
        {
            // Create nuspec
            var packageId = Path.GetFileNameWithoutExtension(fileName);
            var nuspecContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>{packageId}</id>
    <version>1.0.0</version>
    <authors>Test</authors>
    <description>Test package with multiple DLLs</description>
  </metadata>
</package>";
            File.WriteAllText(Path.Combine(tempExtractDir, $"{packageId}.nuspec"), nuspecContent);

            // Create multiple DLLs in different folders
            var libDir = Path.Combine(tempExtractDir, "lib", "net10.0");
            Directory.CreateDirectory(libDir);
            File.WriteAllText(Path.Combine(libDir, "Library1.dll"), "dll1");
            File.WriteAllText(Path.Combine(libDir, "Library2.dll"), "dll2");

            var toolsDir = Path.Combine(tempExtractDir, "tools");
            Directory.CreateDirectory(toolsDir);
            File.WriteAllText(Path.Combine(toolsDir, "Tool.exe"), "exe");

            ZipFile.CreateFromDirectory(tempExtractDir, packagePath, CompressionLevel.Optimal, false);

            return packagePath;
        }
        finally
        {
            if (Directory.Exists(tempExtractDir))
            {
                Directory.Delete(tempExtractDir, true);
            }
        }
    }

    private string CreateTestNuGetPackageWithMixedDlls(string fileName)
    {
        var packagePath = Path.Combine(_testDirectory, fileName);
        var tempExtractDir = Path.Combine(_testDirectory, $"temp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempExtractDir);

        try
        {
            // Create nuspec
            var packageId = Path.GetFileNameWithoutExtension(fileName);
            var nuspecContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>{packageId}</id>
    <version>1.0.0</version>
    <authors>Test</authors>
    <description>Test package with own and third-party DLLs</description>
  </metadata>
</package>";
            File.WriteAllText(Path.Combine(tempExtractDir, $"{packageId}.nuspec"), nuspecContent);

            // Create DLLs - some "own" files and some "third-party" files
            var libDir = Path.Combine(tempExtractDir, "lib", "net10.0");
            Directory.CreateDirectory(libDir);

            // Own files (matching ISynergy.* pattern)
            File.WriteAllText(Path.Combine(libDir, "ISynergy.Framework.dll"), "own dll 1");
            File.WriteAllText(Path.Combine(libDir, "ISynergy.Core.dll"), "own dll 2");

            // Third-party files (should not match ISynergy.* pattern)
            File.WriteAllText(Path.Combine(libDir, "Newtonsoft.Json.dll"), "third party dll 1");
            File.WriteAllText(Path.Combine(libDir, "ThirdParty.dll"), "third party dll 2");

            ZipFile.CreateFromDirectory(tempExtractDir, packagePath, CompressionLevel.Optimal, false);

            return packagePath;
        }
        finally
        {
            if (Directory.Exists(tempExtractDir))
            {
                Directory.Delete(tempExtractDir, true);
            }
        }
    }
}
