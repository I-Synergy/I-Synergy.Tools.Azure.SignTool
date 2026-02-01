namespace ISynergy.Tools.Azure.SignTool.Core.Enumerations;

[type: Flags]
internal enum CertCloreStoreFlags : uint
{
    NONE = 0,
    CERT_CLOSE_STORE_FORCE_FLAG = 0x00000001,
    CERT_CLOSE_STORE_CHECK_FLAG = 0x00000002,
}
