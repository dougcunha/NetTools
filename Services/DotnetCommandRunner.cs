using Spectre.Console;
using System.Diagnostics;

namespace NetTools.Services;

/// <summary>
/// Service responsible for running dotnet CLI commands in the solution context.
/// </summary>
public sealed class DotnetCommandRunner(IAnsiConsole console, IProcessRunner processRunner) 
{
    /// <summary>
    /// Executes 'dotnet clean' in the specified solution directory.
    /// </summary>
    /// <param name="solutionDirectory">The path to the solution directory.</param>
    /// <param name="solutionOrProjectFile">The solution or project file to clean (optional).</param>
    public bool Clean(string solutionDirectory, string? solutionFile = null, bool verbose = false)
        => RunDotnetCommand("clean", solutionDirectory, solutionFile, verbose);

    /// <summary>
    /// Executes 'dotnet restore' in the specified solution directory.
    /// </summary>
    /// <param name="solutionDirectory">The path to the solution directory.</param>
    /// <param name="solutionOrProjectFile">The solution or project file to restore (optional).</param>
    public bool Restore(string solutionDirectory, string? solutionFile = null, bool verbose = false)
        => RunDotnetCommand("restore", solutionDirectory, solutionFile, verbose);

    /// <summary>
    /// Executes 'dotnet build' in the specified solution directory.
    /// </summary>
    /// <param name="solutionDirectory">The path to the solution directory.</param>
    /// <param name="solutionOrProjectFile">The solution or project file to build (optional).</param>
    public bool Build(string solutionDirectory, string? solutionFile = null, bool verbose = false)
        => RunDotnetCommand("build", solutionDirectory, solutionFile, verbose);

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
    public bool RunSequentialCommands(string solutionDirectory, string? solutionFile = null, bool verbose = false,bool clean = false, bool restore = false, bool build = false)
    {
        if (clean)
        {
            console.MarkupLine("[yellow]Cleaning the solution...[/]");

            if (!Clean(solutionDirectory, solutionFile, verbose))
            {
                console.MarkupLine("[red]Failed to clean the solution.[/]");

                return false;
            }
        }

        if (restore)
        {
            console.MarkupLine("[yellow]Restoring the solution...[/]");

            if (!Restore(solutionDirectory, solutionFile, verbose))
            {
                console.MarkupLine("[red]Failed to restore the solution.[/]");

                return false;
            }
        }

        if (build)
        {
            console.MarkupLine("[yellow]Building the solution...[/]");

            if (!Build(solutionDirectory, solutionFile, verbose))
            {
                console.MarkupLine("[red]Failed to build the solution.[/]");

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
    private bool RunDotnetCommand(string command, string workingDirectory, string? solutionOrProjectFile = null, bool verbose = false)
    {
        var argument = solutionOrProjectFile is not null ? $"{command} \"{solutionOrProjectFile}\"" : command;
        var success = false;

        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start($"dotnet {argument}...", ctx =>
            {                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = argument,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };


                void OutputDataReceived(object s, DataReceivedEventArgs e)
                {
                    if (verbose && !string.IsNullOrWhiteSpace(e.Data))
                        console.WriteLine(e.Data);
                }

                void ErrorDataReceived(object s, DataReceivedEventArgs e)
                {
                    if (verbose && !string.IsNullOrWhiteSpace(e.Data))
                        console.MarkupLineInterpolated($"[red]{e.Data}[/]");
                }

                success = processRunner.Run(startInfo, OutputDataReceived, ErrorDataReceived) == 0; 
            });

        return success;
    }
}
