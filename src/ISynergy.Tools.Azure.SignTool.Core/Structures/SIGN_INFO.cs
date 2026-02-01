using System.Runtime.InteropServices;

namespace ISynergy.Tools.Azure.SignTool.Core.Structures;

[type: StructLayout(LayoutKind.Sequential)]
internal readonly struct SIGN_INFO
{
    public readonly uint cbSize;

    public readonly IntPtr callback;

    public readonly IntPtr pvOpaque;

    public SIGN_INFO(IntPtr callback)
    {
        cbSize = (uint)Marshal.SizeOf<SIGN_INFO>();
        this.callback = callback;
        pvOpaque = default;
    }
}
