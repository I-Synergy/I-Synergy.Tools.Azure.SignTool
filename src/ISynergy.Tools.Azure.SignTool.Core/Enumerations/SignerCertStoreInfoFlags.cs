namespace ISynergy.Tools.Azure.SignTool.Core.Enumerations;

[type: Flags]
internal enum SignerCertStoreInfoFlags
{

    SIGNER_CERT_POLICY_CHAIN = 0x02,
    SIGNER_CERT_POLICY_CHAIN_NO_ROOT = 0x08,
    SIGNER_CERT_POLICY_STORE = 0x01
}
