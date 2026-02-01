using ISynergy.Tools.Azure.SignTool.Core.Configurations;
using ISynergy.Tools.Azure.SignTool.Core.Enumerations;
using ISynergy.Tools.Azure.SignTool.Core.Services;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ISynergy.Tools.Azure.SignTool.Core.Tests.Services;

[TestClass]
public class CodeSigningServiceTests
{
    private DirectoryInfo? _scratchDirectory;

    [TestInitialize]
    public void TestInitialize()
    {
        var directory = Path.Join(Path.GetTempPath(), "ISynergy.Tools.Azure.SignTool.Core.Tests");
        _scratchDirectory = Directory.CreateDirectory(directory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scratchDirectory?.Delete(true);
    }

    [TestMethod]
    [DataRow("certificates/sign/rsa-2048.pfx")]
    [DataRow("certificates/sign/rsa-4096.pfx")]
    public void ShouldSignExeWithRSASigningCertificates_Sha1FileDigest(string certificate)
    {
        var signingCert = X509CertificateLoader.LoadPkcs12FromFile(certificate, "test", X509KeyStorageFlags.EphemeralKeySet);
        var signer = new CodeSigningService(signingCert.GetRSAPrivateKey()!, signingCert, HashAlgorithmName.SHA1, TimeStampConfiguration.None);
        var fileToSign = GetFileToSign();
        var result = signer.SignFile(fileToSign, null, null, null);
        Assert.AreEqual(0, result);
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348))
        {
            result = signer.SignFile(fileToSign, null, null, null, appendSignature: true);
            Assert.AreEqual(0, result);
            result = signer.SignFile(fileToSign, null, null, null, appendSignature: false);
            Assert.AreEqual(0, result);
        }
    }

    [TestMethod]
    [DataRow("certificates/sign/rsa-2048.pfx")]
    [DataRow("certificates/sign/rsa-4096.pfx")]
    public void ShouldSignExeWithRSASigningCertificates_Sha256FileDigest(string certificate)
    {
        var signingCert = X509CertificateLoader.LoadPkcs12FromFile(certificate, "test", X509KeyStorageFlags.EphemeralKeySet);
        var signer = new CodeSigningService(signingCert.GetRSAPrivateKey()!, signingCert, HashAlgorithmName.SHA256, TimeStampConfiguration.None);
        var fileToSign = GetFileToSign();
        var result = signer.SignFile(fileToSign, null, null, null);
        Assert.AreEqual(0, result);
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348))
        {
            result = signer.SignFile(fileToSign, null, null, null, appendSignature: true);
            Assert.AreEqual(0, result);
            result = signer.SignFile(fileToSign, null, null, null, appendSignature: false);
            Assert.AreEqual(0, result);
        }
    }

    [TestMethod]
    [DataRow("certificates/sign/ecdsa-nist-p256.pfx")]
    [DataRow("certificates/sign/ecdsa-nist-p384.pfx")]
    [DataRow("certificates/sign/ecdsa-nist-p521.pfx")]
    public void ShouldSignExeWithECDsaSigningCertificates_Sha256FileDigest(string certificate)
    {
        var signingCert = X509CertificateLoader.LoadPkcs12FromFile(certificate, "test", X509KeyStorageFlags.EphemeralKeySet);
        var signer = new CodeSigningService(signingCert.GetECDsaPrivateKey()!, signingCert, HashAlgorithmName.SHA256, TimeStampConfiguration.None);
        var fileToSign = GetFileToSign();
        var result = signer.SignFile(fileToSign, null, null, null);
        Assert.AreEqual(0, result);
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348))
        {
            result = signer.SignFile(fileToSign, null, null, null, appendSignature: true);
            Assert.AreEqual(0, result);
            result = signer.SignFile(fileToSign, null, null, null, appendSignature: false);
            Assert.AreEqual(0, result);
        }
    }

    [TestMethod]
    [DataRow("certificates/sign/ecdsa-nist-p256.pfx")]
    [DataRow("certificates/sign/ecdsa-nist-p384.pfx")]
    [DataRow("certificates/sign/ecdsa-nist-p521.pfx")]
    public void ShouldSignExeWithECDsaSigningCertificates_Sha256FileDigest_WithTimestamps(string certificate)
    {
        var signingCert = X509CertificateLoader.LoadPkcs12FromFile(certificate, "test", X509KeyStorageFlags.EphemeralKeySet);
        var timestampConfig = new TimeStampConfiguration("http://timestamp.digicert.com", HashAlgorithmName.SHA256, TimeStampType.RFC3161);
        var signer = new CodeSigningService(signingCert.GetECDsaPrivateKey()!, signingCert, HashAlgorithmName.SHA256, timestampConfig);
        var fileToSign = GetFileToSign();
        var result = signer.SignFile(fileToSign, null, null, null);
        Assert.AreEqual(0, result);
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348))
        {
            result = signer.SignFile(fileToSign, null, null, null, appendSignature: true);
            Assert.AreEqual(0, result);
            result = signer.SignFile(fileToSign, null, null, null, appendSignature: false);
            Assert.AreEqual(0, result);
        }
    }

    [TestMethod]
    [DataRow("certificates/sign/rsa-2048.pfx")]
    [DataRow("certificates/sign/rsa-4096.pfx")]
    public void ShouldSignExeWithRSASigningCertificates_Sha256FileDigest_WithTimestamps(string certificate)
    {
        var signingCert = X509CertificateLoader.LoadPkcs12FromFile(certificate, "test", X509KeyStorageFlags.EphemeralKeySet);
        var timestampConfig = new TimeStampConfiguration("http://timestamp.digicert.com", HashAlgorithmName.SHA256, TimeStampType.RFC3161);
        var signer = new CodeSigningService(signingCert.GetRSAPrivateKey()!, signingCert, HashAlgorithmName.SHA256, timestampConfig);
        var fileToSign = GetFileToSign();
        var result = signer.SignFile(fileToSign, null, null, null);
        Assert.AreEqual(0, result);
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348))
        {
            result = signer.SignFile(fileToSign, null, null, null, appendSignature: true);
            Assert.AreEqual(0, result);
            result = signer.SignFile(fileToSign, null, null, null, appendSignature: false);
            Assert.AreEqual(0, result);
        }
    }

    private string GetFileToSign()
    {
        var guid = Guid.NewGuid();
        var path = Path.Combine(_scratchDirectory!.FullName, $"{guid}.exe");
        File.Copy("signtarget.exe", path);
        return path;
    }

}
