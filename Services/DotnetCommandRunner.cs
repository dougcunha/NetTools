using Spectre.Console;
using System.Diagnostics;

namespace NetTools.Services;

/// <summary>
/// Service responsible for running dotnet CLI commands in the solution context.
/// </summary>
public sealed class DotnetCommandRunner
{
    private readonly string? _solutionFile;
    private readonly bool _verbose;
    private readonly string _solutionDirectory;

    public DotnetCommandRunner(string solutionDirectory, string? solutionFile = null, bool verbose = false)
    {
        _solutionDirectory = solutionDirectory;
        _solutionFile = solutionFile;
        _verbose = verbose;
    }

    /// <summary>
    /// Executes 'dotnet clean' in the specified solution directory.
    /// </summary>
    /// <param name="solutionDirectory">The path to the solution directory.</param>
    /// <param name="solutionOrProjectFile">The solution or project file to clean (optional).</param>
    public bool Clean()
        => RunDotnetCommand("clean", _solutionDirectory, _solutionFile, _verbose);

    /// <summary>
    /// Executes 'dotnet restore' in the specified solution directory.
    /// </summary>
    /// <param name="solutionDirectory">The path to the solution directory.</param>
    /// <param name="solutionOrProjectFile">The solution or project file to restore (optional).</param>
    public bool Restore()
        => RunDotnetCommand("restore", _solutionDirectory, _solutionFile, _verbose);

    /// <summary>
    /// Executes 'dotnet build' in the specified solution directory.
    /// </summary>
    /// <param name="solutionDirectory">The path to the solution directory.</param>
    /// <param name="solutionOrProjectFile">The solution or project file to build (optional).</param>
    public bool Build()
        => RunDotnetCommand("build", _solutionDirectory, _solutionFile, _verbose);

    /// <summary>
    /// Runs a sequence of dotnet CLI commands based on the provided options.
    /// </summary>
    /// <param name="clean">If true, runs 'dotnet clean'.</param>
    /// <param name="restore">If true, runs 'dotnet restore'.</param>
    /// <param name="build">If true, runs 'dotnet build'.</param>
    /// <remarks>
    /// This method runs the commands in the order: clean, restore, build.
    /// If any command fails, the subsequent commands are not executed.
    /// </remarks>    
    public bool RunSequentialCommands(bool clean, bool restore, bool build)
    {
        if (clean)
        {
            AnsiConsole.MarkupLine("[yellow]Cleaning the solution...[/]");

            if (!Clean())
            {
                AnsiConsole.MarkupLine("[red]Failed to clean the solution.[/]");

                return false;
            }
        }

        if (restore)
        {
            AnsiConsole.MarkupLine("[yellow]Restoring the solution...[/]");

            if (!Restore())
            {
                AnsiConsole.MarkupLine("[red]Failed to restore the solution.[/]");

                return false;
            }
        }

        if (build)
        {
            AnsiConsole.MarkupLine("[yellow]Building the solution...[/]");
            
            if (!Build())
            {
                AnsiConsole.MarkupLine("[red]Failed to build the solution.[/]");

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Runs a dotnet CLI command in the given directory and prints output to the console.
    /// </summary>
    /// <param name="command">The dotnet command to run (e.g., 'clean', 'restore').</param>
    /// <param name="workingDirectory">The directory to run the command in.</param>
    /// <param name="solutionOrProjectFile">The solution or project file to target (optional).</param>
    /// <param name="verbose">Indicates whether verbose output is enabled.</param>
    private static bool RunDotnetCommand(string command, string workingDirectory, string? solutionOrProjectFile = null, bool verbose = false)
    {
        var argument = solutionOrProjectFile is not null ? $"{command} \"{solutionOrProjectFile}\"" : command;
        var success = false;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start($"dotnet {argument}...", ctx =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = argument,
                        WorkingDirectory = workingDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.OutputDataReceived += (s, e) =>
                {
                    if (verbose && !string.IsNullOrWhiteSpace(e.Data))
                        AnsiConsole.WriteLine(e.Data);
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (verbose && !string.IsNullOrWhiteSpace(e.Data))
                        AnsiConsole.MarkupLineInterpolated($"[red]{e.Data}[/]");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                success = process.ExitCode == 0;
            });

        return success;
    }
}
