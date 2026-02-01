using ISynergy.Tools.Azure.SignTool.Core.Enumerations;
using ISynergy.Tools.Azure.SignTool.Core.Factories;

namespace ISynergy.Tools.Azure.SignTool.Core.Tests.Factories;

[TestClass]
public class SipExtensionFactoryTests
{
    [TestMethod]
    [DataRow(@"C:\foo.appx")]
    [DataRow(@"C:\foo.APPX")]
    [DataRow(@"C:\foo.eappx")]
    [DataRow(@"C:\foo.eaPPx")]
    [DataRow(@"C:\foo.appxbundle")]
    [DataRow(@"C:\foo.appxBUNDLE")]
    [DataRow(@"C:\foo.eappxbundle")]
    [DataRow(@"C:\foo.EAppxBUNDLE")]
    public void ShouldReturnAppxSipForAppxFiles(string path)
    {
        var kind = SipExtensionFactory.GetSipKind(path);
        Assert.AreEqual(SipKind.Appx, kind);
    }

    [TestMethod]
    [DataRow(@"C:\foo.msix")]
    [DataRow(@"C:\foo.MSIX")]
    [DataRow(@"C:\foo.emsix")]
    [DataRow(@"C:\foo.emSIx")]
    [DataRow(@"C:\foo.msixbundle")]
    [DataRow(@"C:\foo.msixBUNDLE")]
    [DataRow(@"C:\foo.emsixbundle")]
    [DataRow(@"C:\foo.EMSixBUNDLE")]
    public void ShouldReturnAppxSipForMsixFiles(string path)
    {
        var kind = SipExtensionFactory.GetSipKind(path);
        Assert.AreEqual(SipKind.Appx, kind);
    }

    [TestMethod]
    [DataRow(@"C:\foo.exe")]
    [DataRow(@"C:\foo.msi")]
    [DataRow(@"C:\foo.cab")]
    [DataRow(@"C:\foo.dll")]
    [DataRow(@"C:\foo.bin")]
    public void ShouldReturnNoneForOtherFileTypes(string path)
    {
        var kind = SipExtensionFactory.GetSipKind(path);
        Assert.AreEqual(SipKind.None, kind);
    }

    [TestMethod]
    [DataRow(@"C:\foo.nupkg")]
    [DataRow(@"C:\foo.NUPKG")]
    [DataRow(@"C:\foo.snupkg")]
    [DataRow(@"C:\foo.SNUPKG")]
    public void ShouldReturnNuGetForNuGetPackages(string path)
    {
        var kind = SipExtensionFactory.GetSipKind(path);
        Assert.AreEqual(SipKind.NuGet, kind);
    }
}
