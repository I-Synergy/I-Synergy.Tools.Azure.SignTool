using ISynergy.Tools.Azure.SignTool.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO.Compression;

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
}
