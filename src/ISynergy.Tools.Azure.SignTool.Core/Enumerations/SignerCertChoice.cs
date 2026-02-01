namespace ISynergy.Tools.Azure.SignTool.Core.Enumerations;

internal enum SignerCertChoice : uint
{
    SIGNER_CERT_SPC_FILE = 1,
    SIGNER_CERT_STORE = 2,
    SIGNER_CERT_SPC_CHAIN = 3
}