using System.Runtime.InteropServices;

namespace ISynergy.Tools.Azure.SignTool.Core.Structures;

[type: StructLayout(LayoutKind.Sequential)]
internal struct CRYPTOAPI_BLOB
{
    public uint cbData;
    public IntPtr pbData;
}
