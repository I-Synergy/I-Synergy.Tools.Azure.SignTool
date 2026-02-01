using ISynergy.Tools.Azure.SignTool.Commands;
using ISynergy.Tools.Azure.SignTool.Results;
using System.CommandLine;
using System.Reflection;

namespace ISynergy.Tools.Azure.SignTool;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("Azure Sign Tool is only supported on Windows.");
            return HRESULT.E_PLATFORMNOTSUPPORTED;
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(10))
        {
            Console.Error.WriteLine("Azure Sign Tool requires Windows 10 or later.");
            return HRESULT.E_PLATFORMNOTSUPPORTED;
        }

        var rootCommand = new RootCommand("Azure Sign Tool - Code signing tool using Azure Key Vault");

        rootCommand.Add(SignCommand.Create());
        rootCommand.Add(AboutCommand.Create());

        var config = new CommandLineConfiguration(rootCommand);
        return await config.InvokeAsync(args);
    }

    internal static string GetVersion()
    {
        Assembly assembly = typeof(Program).Assembly;
        string? version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrEmpty(version))
        {
            version = assembly.GetName().Version?.ToString();
        }

        return version ?? "0.0.0";
    }
}
