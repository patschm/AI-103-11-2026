using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;

namespace DevToolsServer;

[McpServerToolType]
public class ShellTools
{
    public const string DefaultWorkFolder = @"D:\Gen";

    [McpServerTool(Name = "execute_dotnet_command", Destructive = true, OpenWorld = false),
     Description("Run a dotnet CLI command and return its exit code, standard output and standard error.")]
    public static async Task<string> ExecuteDotnetCommand(
        [Description("Arguments after 'dotnet', for example 'new console -n MyApp' or 'add package Newtonsoft.Json'. Apply --force if a command fails because files exist.")] string arguments,
        [Description("Working directory in which to run the command. Created when missing.")] string workingDirectory = DefaultWorkFolder)
    {
        // Be forgiving when the model includes the 'dotnet' prefix.
        arguments = arguments.Trim();
        if (arguments.StartsWith("dotnet ", StringComparison.OrdinalIgnoreCase))
        {
            arguments = arguments["dotnet ".Length..];
        }

        try
        {
            Directory.CreateDirectory(workingDirectory);

            var processInfo = new ProcessStartInfo("dotnet", arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // Never let the child process read the MCP stdio stream.
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = new Process { StartInfo = processInfo };
            process.Start();
            process.StandardInput.Close();

            // Read both streams concurrently to avoid a deadlock when one buffer fills up.
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                return $"Error: 'dotnet {arguments}' timed out after 2 minutes and was stopped.";
            }

            string output = await outputTask;
            string error = await errorTask;
            return $"Exit code: {process.ExitCode}\nOutput:\n{output}\nErrors:\n{error}";
        }
        catch (Exception e)
        {
            return $"Error running 'dotnet {arguments}': {e.Message}";
        }
    }
}
