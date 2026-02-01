using System.Text.RegularExpressions;

namespace ISynergy.Tools.Azure.SignTool.Tests;

[TestClass]
public class HelpTests
{
    private static readonly SemaphoreSlim _sync = new(1, 1);

    [TestMethod]
    public async Task BlankInputShouldShowHelpOutput()
    {
        (string StdOut, string StdErr, int ExitCode) = await Capture(async () =>
        {
            return await Program.Main([]);
        });

        // System.CommandLine shows "Required command was not provided." when no command is given
        Assert.IsTrue(StdErr.Contains("Required command was not provided", StringComparison.OrdinalIgnoreCase),
            $"Expected stderr to contain 'Required command was not provided', got: {StdErr}");
        Assert.AreEqual(1, ExitCode);
    }

    [TestMethod]
    public async Task BlankInputForSignCommandShouldShowHelpOutput()
    {
        (string StdOut, string StdErr, int ExitCode) = await Capture(async () =>
        {
            return await Program.Main(["sign"]);
        });

        // When sign command is invoked without required parameters, validation errors are shown
        Assert.IsTrue(StdErr.Contains("--azure-key-vault-url is required"),
            $"Expected stderr to contain '--azure-key-vault-url is required', got: {StdErr}");
        Assert.AreNotEqual(0, ExitCode);
    }

    [TestMethod]
    public async Task ShowVersionOnOutputVersionArg()
    {
        (string StdOut, string StdErr, int ExitCode) = await Capture(async () =>
        {
            return await Program.Main(["--version"]);
        });

        Assert.IsTrue(Regex.IsMatch(StdOut, @"^\d+\.\d+\.\d+"), $"Expected version pattern, got: {StdOut}");
        Assert.AreEqual(0, ExitCode);
    }

    private static async Task<(string StdOut, string StdErr, T Result)> Capture<T>(Func<ValueTask<T>> act)
    {
        try
        {
            await _sync.WaitAsync();

            TextWriter oldStdOutWriter = Console.Out;
            TextWriter oldStdErrWriter = Console.Error;
            StringWriter stdOutWriter = new StringWriter();
            StringWriter stdErrWriter = new StringWriter();

            try
            {
                Console.SetOut(stdOutWriter);
                Console.SetError(stdErrWriter);
                T result = await act();
                return (stdOutWriter.ToString(), stdErrWriter.ToString(), result);
            }
            finally
            {
                Console.SetOut(oldStdOutWriter);
                Console.SetError(oldStdErrWriter);
            }
        }
        finally
        {
            _sync.Release();
        }
    }
}
