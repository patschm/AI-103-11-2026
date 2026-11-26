using System.ComponentModel;
using System.Diagnostics;

namespace Agents.Tools;

public class ShellTools
{
    [Description("A function that executes dotnet cli commands in the specified folder (path)")]
    public static async Task ExecuteDotnetCommand(
        [Description("Full command to run (e.g. 'dotnet new', 'dotnet add'). Apply --force if fails")]string command, 
        [Description("Optional working directory")]string path = "WorkFolder")
    {
        var processInfo = new ProcessStartInfo()
        {
            FileName = "dotnet.exe",
            Arguments = command,
            WorkingDirectory = path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var process = new Process
        {
            StartInfo = processInfo
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new Exception($"Command '{command}' failed with exit code {process.ExitCode}: {error}");
        }
        Console.WriteLine(output);
    }

    [Description("A function that executes generic shell commands in the specified folder (path)")]
    public static async Task ExecuteShellCommand(
        [Description("Full command to run (e.g. echo, dir, etc). Apply --force if fails")] string command,
        [Description("Optional working directory")] string path = "WorkFolder")
    {
        var processInfo = new ProcessStartInfo()
        {
            FileName = "cmd.exe",
            Arguments = command,
            WorkingDirectory = path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var process = new Process
        {
            StartInfo = processInfo
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new Exception($"Command '{command}' failed with exit code {process.ExitCode}: {error}");
        }
        Console.WriteLine(output);
    }
}
