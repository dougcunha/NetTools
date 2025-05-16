using System.CommandLine;
using NetTools.Services;
using Spectre.Console;

namespace NetTools.Commands;

/// <summary>
/// Command to remove a NuGet package from selected projects in a solution.
/// </summary>
public sealed class RemoveCommand : Command
{
    public RemoveCommand() : base("rm", "Remove a NuGet package from selected projects in a solution.")
    {
        var packageIdArgument = new Argument<string>("packageId", "The NuGet package id to remove.");

        var solutionFileArgument = new Argument<string?>
        (
            "solutionFile",
            () => SolutionExplorer.GetOrPromptSolutionFile(null),
            "The path to the .sln file to discover projects (optional). If omitted, the tool will search for a solution file in the current directory or prompt for selection."
        );

        var cleanOption = new Option<bool>(["--clean", "-c"], () => false, "Clean the solution after removal.");
        var restoreOption = new Option<bool>(["--restore", "-r"], () => false, "Restore the solution after removal.");
        var buildOption = new Option<bool>(["--build", "-b"], () => false, "Build the solution after removal.");
        var verboseOption = new Option<bool>(["--verbose", "-v"], () => false, "Show detailed output of dotnet commands.");

        AddArgument(packageIdArgument);
        AddArgument(solutionFileArgument);
        AddOption(cleanOption);
        AddOption(restoreOption);
        AddOption(buildOption);
        AddOption(verboseOption);

        this.SetHandler((packageId, solutionFile, clean, restore, build, verbose) =>
        {
            var projectsWithPackage = SolutionExplorer.DiscoverAndSelectProjects
            (
                solutionFile,
                $"[green]Select the projects to remove package » {packageId}:[/]",
                "[yellow]No .csproj files found in the solution file with the specified package.[/]",
                csproj => NugetVersionStandardizer.HasPackage(csproj, packageId)
            );

            if (projectsWithPackage.Count == 0)
                return;

            var solutionDir = Path.GetDirectoryName(solutionFile ?? string.Empty)!;

            foreach (var relativePath in projectsWithPackage)
            {
                var csprojPath = Path.Combine(solutionDir, relativePath);
                NugetVersionStandardizer.RemovePackageFromCsproj(csprojPath, packageId);
                AnsiConsole.MarkupLine($"[green]Removed '{packageId}' from {relativePath}.[/]");
            }

            var solutionName = Path.GetFileName(solutionFile);
            var dotnetRunner = new DotnetCommandRunner(solutionDir, solutionName, verbose);
            dotnetRunner.RunSequentialCommands(clean, restore, build);
        }, packageIdArgument, solutionFileArgument, cleanOption, restoreOption, buildOption, verboseOption);
    }
}
