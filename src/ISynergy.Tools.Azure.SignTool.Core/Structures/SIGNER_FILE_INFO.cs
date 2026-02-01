using System.Runtime.InteropServices;

namespace ISynergy.Tools.Azure.SignTool.Core.Structures;

[type: StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct SIGNER_FILE_INFO
{
    public readonly uint cbSize;
    public readonly char* pwszFileName;
    public readonly IntPtr hFile;

    public SIGNER_FILE_INFO(char* pwszFileName, IntPtr hFile)
    {
        cbSize = (uint)Marshal.SizeOf<SIGNER_FILE_INFO>();
        this.pwszFileName = pwszFileName;
        this.hFile = hFile;
    }
}
