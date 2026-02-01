using ISynergy.Tools.Azure.SignTool.Core.Enumerations;
using System.Runtime.InteropServices;

namespace ISynergy.Tools.Azure.SignTool.Core.Structures;

[type: StructLayout(LayoutKind.Sequential)]
internal unsafe struct SIGNER_SIGN_EX3_PARAMS
{
    public SignerSignEx3Flags dwFlags;
    public SIGNER_SUBJECT_INFO* pSubjectInfo;
    public SIGNER_CERT* pSignerCert;
    public SIGNER_SIGNATURE_INFO* pSignatureInfo;
    public IntPtr pProviderInfo;
    public SignerSignTimeStampFlags dwTimestampFlags;
    public byte* pszTimestampAlgorithmOid;
    public char* pwszHttpTimeStamp;
    public IntPtr psRequest;
    public SIGN_INFO* pSignCallBack;
    public IntPtr* ppSignerContext;
    public IntPtr pCryptoPolicy;
    public IntPtr pReserved;
}