using ISynergy.Tools.Azure.SignTool.Core.Translators;
using System.Security.Cryptography;
using System.Text;

namespace ISynergy.Tools.Azure.SignTool.Core.Tests.Translators;

[TestClass]
public class AlgorithmTranslatorTests
{
    [TestMethod]
    [DataRow("SHA1", 0x00008004u)]
    [DataRow("SHA256", 0x0000800cu)]
    [DataRow("SHA384", 0x0000800du)]
    [DataRow("SHA512", 0x0000800eu)]
    public void ShouldTranslateNameToCAPIAlgId(string algorithmName, uint algId)
    {
        var name = new HashAlgorithmName(algorithmName);
        Assert.IsNotNull(name.Name, $"HashAlgorithmName.Name should not be null for {algorithmName}");
        Assert.AreEqual(algId, AlgorithmTranslator.HashAlgorithmToAlgId(name));
    }

    [TestMethod]
    [DataRow("SHA1", "1.3.14.3.2.26")]
    [DataRow("SHA256", "2.16.840.1.101.3.4.2.1")]
    [DataRow("SHA384", "2.16.840.1.101.3.4.2.2")]
    [DataRow("SHA512", "2.16.840.1.101.3.4.2.3")]
    public void ShouldTranslateNameToAsciiEncodedNullTerminatedOID(string algorithmName, string oid)
    {
        var name = new HashAlgorithmName(algorithmName);
        Span<byte> expectedBytes = stackalloc byte[oid.Length + 1];
        expectedBytes.Fill(0);
        Encoding.ASCII.GetBytes(oid, expectedBytes);
        var actualBytes = AlgorithmTranslator.HashAlgorithmToOidAsciiTerminated(name);
        Assert.IsTrue(expectedBytes.SequenceEqual(actualBytes), "ExpectedBytes do not equal actual bytes.");
    }

    [TestMethod]
    public void ShouldThrowNotSupportExceptionForUnknownAlgId()
    {
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            AlgorithmTranslator.HashAlgorithmToAlgId(default);
        });
    }

    [TestMethod]
    public void ShouldThrowNotSupportExceptionForUnknownOID()
    {
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            AlgorithmTranslator.HashAlgorithmToOidAsciiTerminated(default);
        });
    }

    [TestMethod]
    public void VerifyHashAlgorithmNamePropertiesAreNotNull()
    {
        // Verify that the HashAlgorithmName static properties return valid names
        Assert.IsNotNull(HashAlgorithmName.SHA1.Name, "SHA1.Name should not be null");
        Assert.IsNotNull(HashAlgorithmName.SHA256.Name, "SHA256.Name should not be null");
        Assert.IsNotNull(HashAlgorithmName.SHA384.Name, "SHA384.Name should not be null");
        Assert.IsNotNull(HashAlgorithmName.SHA512.Name, "SHA512.Name should not be null");
        
        Assert.AreEqual("SHA1", HashAlgorithmName.SHA1.Name);
        Assert.AreEqual("SHA256", HashAlgorithmName.SHA256.Name);
        Assert.AreEqual("SHA384", HashAlgorithmName.SHA384.Name);
        Assert.AreEqual("SHA512", HashAlgorithmName.SHA512.Name);
    }
}
