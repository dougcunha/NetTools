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
        var solutionFileArgument = new Argument<string>("solutionFile", "The path to the .sln file to discover projects.");
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
                csproj => NugetVersionStandardizer.HasPackage(csproj, packageId)
            );

            var solutionDir = Path.GetDirectoryName(solutionFile)!;

            if (projectsWithPackage.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No project selected.[/]");

                return;
            }

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
