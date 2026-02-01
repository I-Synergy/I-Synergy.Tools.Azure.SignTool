using System.Runtime.InteropServices;

namespace ISynergy.Tools.Azure.SignTool.Core.Structures;

[type: StructLayout(LayoutKind.Explicit)]
internal readonly unsafe struct SIGNER_SUBJECT_INFO_UNION
{
    [FieldOffset(0)]
    public readonly SIGNER_FILE_INFO* file;

    public SIGNER_SUBJECT_INFO_UNION(SIGNER_FILE_INFO* file)
    {
        this.file = file;
    }
}

