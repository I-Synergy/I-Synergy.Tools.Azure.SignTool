using ISynergy.Tools.Azure.SignTool.Core.Enumerations;
using System.Runtime.InteropServices;

namespace ISynergy.Tools.Azure.SignTool.Core.Interop;

internal static partial class crypt32
{
    [LibraryImport(nameof(crypt32), SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CertCloseStore
    (
        IntPtr hCertStore,
        CertCloreStoreFlags dwFlags
    );

    [LibraryImport(nameof(crypt32), SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr CertOpenStore
    (
        string lpszStoreProvider,
        CertEncodingType CertEncodingType,
        IntPtr hCryptProv,
        CertOpenStoreFlags dwFlags,
        IntPtr pvPara
    );
}