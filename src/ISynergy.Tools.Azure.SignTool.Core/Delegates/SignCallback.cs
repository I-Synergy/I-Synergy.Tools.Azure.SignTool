using ISynergy.Tools.Azure.SignTool.Core.Structures;
using System.Runtime.InteropServices;

namespace ISynergy.Tools.Azure.SignTool.Core.Delegates;

[type: UnmanagedFunctionPointer(CallingConvention.Winapi)]
internal delegate int SignCallback(
    [param: In, MarshalAs(UnmanagedType.SysInt)] IntPtr pCertContext,
    [param: In, MarshalAs(UnmanagedType.SysInt)] IntPtr pvExtra,
    [param: In, MarshalAs(UnmanagedType.U4)] uint algId,
    [param: In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 4)] byte[] pDigestToSign,
    [param: In, MarshalAs(UnmanagedType.U4)] uint dwDigestToSign,
    [param: In, Out] ref CRYPTOAPI_BLOB blob
    );