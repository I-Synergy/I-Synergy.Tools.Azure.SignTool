using System.Runtime.InteropServices;

namespace ISynergy.Tools.Azure.SignTool.Core.Structures;

[type: StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct SIGNER_ATTR_AUTHCODE
{
    public readonly uint cbSize;
    public readonly uint fCommercial;
    public readonly uint fIndividual;

    public readonly char* pwszName;
    public readonly char* pwszInfo;

    public SIGNER_ATTR_AUTHCODE(char* pwszName, char* pwszInfo)
    {
        cbSize = (uint)Marshal.SizeOf<SIGNER_ATTR_AUTHCODE>();
        fCommercial = 0;
        fIndividual = 0;
        this.pwszName = pwszName;
        this.pwszInfo = pwszInfo;
    }
}
