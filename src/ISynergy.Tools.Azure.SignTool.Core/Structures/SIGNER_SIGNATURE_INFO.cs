using ISynergy.Tools.Azure.SignTool.Core.Enumerations;
using System.Runtime.InteropServices;

namespace ISynergy.Tools.Azure.SignTool.Core.Structures;

[type: StructLayout(LayoutKind.Sequential)]
internal readonly struct SIGNER_SIGNATURE_INFO
{
    public readonly uint cbSize;
    public readonly uint algidHash;
    public readonly SignerSignatureInfoAttrChoice dwAttrChoice;
    public readonly SIGNER_SIGNATURE_INFO_UNION attrAuthUnion;
    public readonly IntPtr psAuthenticated;
    public readonly IntPtr psUnauthenticated;

    public SIGNER_SIGNATURE_INFO(uint algidHash,
        SignerSignatureInfoAttrChoice dwAttrChoice,
        SIGNER_SIGNATURE_INFO_UNION attrAuthUnion,
        IntPtr psAuthenticated,
        IntPtr psUnauthenticated
        )
    {
        cbSize = (uint)Marshal.SizeOf<SIGNER_SIGNATURE_INFO>();
        this.algidHash = algidHash;
        this.dwAttrChoice = dwAttrChoice;
        this.attrAuthUnion = attrAuthUnion;
        this.psAuthenticated = psAuthenticated;
        this.psUnauthenticated = psUnauthenticated;
    }
}
