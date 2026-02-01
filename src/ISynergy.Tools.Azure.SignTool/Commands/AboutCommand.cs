using System.CommandLine;

namespace ISynergy.Tools.Azure.SignTool.Commands;

internal static class AboutCommand
{
    public static Command Create()
    {
        var command = new Command("about", "Display information about Azure Sign Tool.");

        command.SetAction((parseResult, cancellationToken) =>
        {
            Console.WriteLine($"Azure Sign Tool v{Program.GetVersion()}");
            Console.WriteLine();
            Console.WriteLine("A tool for signing files using Azure Key Vault certificates.");
            Console.WriteLine();
            Console.WriteLine("For more information, visit:");
            Console.WriteLine("https://github.com/I-Synergy/I-Synergy.Tools.Azure.SignTool");
            return Task.FromResult(0);
        });

        return command;
    }
}
