using ISynergy.Tools.Azure.SignTool.Core.Enumerations;
using ISynergy.Tools.Azure.SignTool.Core.Structures;
using System.Runtime.InteropServices;

namespace ISynergy.Tools.Azure.SignTool.Core.Interop;

internal static partial class mssign32
{
    [LibraryImport(nameof(mssign32), EntryPoint = "SignerSignEx3")]
    public static unsafe partial int SignerSignEx3
    (
        SignerSignEx3Flags dwFlags,
        SIGNER_SUBJECT_INFO* pSubjectInfo,
        SIGNER_CERT* pSignerCert,
        SIGNER_SIGNATURE_INFO* pSignatureInfo,
        IntPtr pProviderInfo,
        SignerSignTimeStampFlags dwTimestampFlags,
        byte* pszTimestampAlgorithmOid,
        char* pwszHttpTimeStamp,
        IntPtr psRequest,
        void* pSipData,
        IntPtr* ppSignerContext,
        IntPtr pCryptoPolicy,
        SIGN_INFO* pSignInfo,
        IntPtr pReserved
    );

    [LibraryImport(nameof(mssign32))]
    public static partial int SignerFreeSignerContext(
        IntPtr pSignerContext
    );
}
