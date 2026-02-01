using ISynergy.Tools.Azure.SignTool.Core.Enumerations;

namespace ISynergy.Tools.Azure.SignTool.Core.Factories;

internal class SipExtensionFactory
{
    public static SipKind GetSipKind(ReadOnlySpan<char> filePath)
    {
        var extension = Path.GetExtension(filePath.ToString());
        if (extension.Equals(".appx", StringComparison.OrdinalIgnoreCase))
        {
            return SipKind.Appx;
        }
        if (extension.Equals(".eappx", StringComparison.OrdinalIgnoreCase))
        {
            return SipKind.Appx;
        }
        if (extension.Equals(".appxbundle", StringComparison.OrdinalIgnoreCase))
        {
            return SipKind.Appx;
        }
        if (extension.Equals(".eappxbundle", StringComparison.OrdinalIgnoreCase))
        {
            return SipKind.Appx;
        }
        if (extension.Equals(".msix", StringComparison.OrdinalIgnoreCase))
        {
            return SipKind.Appx;
        }
        if (extension.Equals(".emsix", StringComparison.OrdinalIgnoreCase))
        {
            return SipKind.Appx;
        }
        if (extension.Equals(".msixbundle", StringComparison.OrdinalIgnoreCase))
        {
            return SipKind.Appx;
        }
        if (extension.Equals(".emsixbundle", StringComparison.OrdinalIgnoreCase))
        {
            return SipKind.Appx;
        }
        if (extension.Equals(".nupkg", StringComparison.OrdinalIgnoreCase))
        {
            return SipKind.NuGet;
        }
        if (extension.Equals(".snupkg", StringComparison.OrdinalIgnoreCase))
        {
            return SipKind.NuGet;
        }
        return SipKind.None;
    }
}
