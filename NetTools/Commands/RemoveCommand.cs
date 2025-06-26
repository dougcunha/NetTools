using System.CommandLine;
using NetTools.Helpers;
using NetTools.Services;
using Spectre.Console;

namespace NetTools.Commands;

/// <summary>
/// Command to remove a NuGet package from selected projects in a solution.
/// </summary>
public sealed class RemoveCommand : Command
{    public RemoveCommand
    (
        IAnsiConsole console,
        ISolutionExplorer solutionExplorer,
        ICsprojHelpers csprojHelpers,
        IDotnetCommandRunner dotnetRunner,
        IEnvironmentService environment
    ) : base("rm", "Remove a NuGet package from selected projects in a solution.")
    {
        var packageIdArgument = new Argument<string>("packageId")
        {
            Description = "The NuGet package id to remove.",
            Arity = ArgumentArity.ExactlyOne
        };

        var solutionFileArgument = new Argument<string?>("solutionFile")
        {
            Description = "The path to the .sln file to discover projects (optional). If omitted, the tool will search for a solution file in the current directory or prompt for selection.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var cleanOption = new Option<bool>("--clean", "-c")
        {
            Description = "Clean the solution after removal."
        };

        var restoreOption = new Option<bool>("--restore", "-r")
        {
            Description = "Restore the solution after removal."
        };

        var buildOption = new Option<bool>("--build", "-b")
        {
            Description = "Build the solution after removal."
        };

        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Show detailed output of dotnet commands."
        };

        Add(packageIdArgument);
        Add(solutionFileArgument);
        Add(cleanOption);
        Add(restoreOption);
        Add(buildOption);
        Add(verboseOption);

        SetAction(result =>
        {
            var packageId = result.GetValue(packageIdArgument)!;
            var solutionFile = result.GetValue(solutionFileArgument);
            var clean = result.GetValue(cleanOption);
            var restore = result.GetValue(restoreOption);
            var build = result.GetValue(buildOption);
            var verbose = result.GetValue(verboseOption);

            solutionFile = solutionExplorer.GetOrPromptSolutionFile(solutionFile);
            var solutionDir = Path.GetDirectoryName(solutionFile ?? string.Empty)!;
            environment.CurrentDirectory = solutionDir;

            var projectsWithPackage = solutionExplorer.DiscoverAndSelectProjects
            (
                solutionFile,
                $"[green]Select the projects to remove package » {packageId}:[/]",
                "[yellow]No .csproj files found in the solution file with the specified package.[/]",
                csproj => csprojHelpers.HasPackage(csproj, packageId)
            );

            if (projectsWithPackage.Count == 0)
                return;

            foreach (var relativePath in projectsWithPackage)
            {
                var csprojPath = Path.Combine(solutionDir, relativePath);
                csprojHelpers.RemovePackageFromCsproj(csprojPath, packageId);
                console.MarkupLine($"[green]Removed '{packageId}' from {relativePath}.[/]");
            }

            var solutionName = Path.GetFileName(solutionFile);
            dotnetRunner.RunSequentialCommands(solutionDir, solutionName, verbose, clean, restore, build);
        });
    }
}
