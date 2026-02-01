using System.Runtime.InteropServices;

namespace ISynergy.Tools.Azure.SignTool.Core.Structures;

[type: StructLayout(LayoutKind.Explicit)]
internal readonly unsafe struct SIGNER_CERT_UNION
{
    public SIGNER_CERT_UNION(SIGNER_CERT_STORE_INFO* certStoreInfo)
    {
        pSpcChainInfo = certStoreInfo;
    }

    [field: FieldOffset(0)]
    public readonly SIGNER_CERT_STORE_INFO* pSpcChainInfo;
}