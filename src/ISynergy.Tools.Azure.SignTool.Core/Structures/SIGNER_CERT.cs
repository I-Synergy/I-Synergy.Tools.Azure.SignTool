using ISynergy.Tools.Azure.SignTool.Core.Enumerations;
using System.Runtime.InteropServices;

namespace ISynergy.Tools.Azure.SignTool.Core.Structures;

[type: StructLayout(LayoutKind.Sequential)]
internal readonly struct SIGNER_CERT
{
    public readonly uint cbSize;
    public readonly SignerCertChoice dwCertChoice;
    public readonly SIGNER_CERT_UNION union;
    public readonly IntPtr hwnd;

    public SIGNER_CERT(SignerCertChoice dwCertChoice, SIGNER_CERT_UNION union)
    {
        this.dwCertChoice = dwCertChoice;
        this.union = union;
        hwnd = default;
        cbSize = (uint)Marshal.SizeOf<SIGNER_CERT>();
    }
}
