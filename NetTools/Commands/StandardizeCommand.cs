using System.CommandLine;
using NetTools.Services;

namespace NetTools.Commands;

/// <summary>
/// Command to standardize NuGet package versions.
/// </summary>
public sealed class StandardizeCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StandardizeCommand"/> class.
    /// </summary>
    public StandardizeCommand
    (
        INugetVersionStandardizer standardizer,
        ISolutionExplorer solutionExplorer,
        IEnvironmentService environment
    ) : base
    (
        "st",
        "Standardize NuGet package versions in a solution."
    )
    {
        var solutionFileArgument = new Argument<string?>("solutionFile")
        {
            Description = "The path to the .sln file to discover projects (optional). If omitted, the tool will search for a solution file in the current directory or prompt for selection.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var cleanOption = new Option<bool>("--clean", "-c")
        {
            Description = "Clean the solution after standardization."
        };

        var restoreOption = new Option<bool>("--restore", "-r")
        {
            Description = "Restore the solution after standardization."
        };

        var buildOption = new Option<bool>("--build", "-b")
        {
            Description = "Build the solution after standardization."
        };

        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Show detailed output of dotnet commands."
        };

        Add(solutionFileArgument);
        Add(cleanOption);
        Add(restoreOption);
        Add(buildOption);
        Add(verboseOption);

        SetAction
        (
            result =>
            {
                var solutionFile = result.GetValue(solutionFileArgument);
                var verbose = result.GetValue(verboseOption);
                var clean = result.GetValue(cleanOption);
                var restore = result.GetValue(restoreOption);
                var build = result.GetValue(buildOption);

                var options = new StandardizeCommandOptions
                {
                    SolutionFile = solutionExplorer.GetOrPromptSolutionFile(solutionFile),
                    Verbose = verbose,
                    Clean = clean,
                    Restore = restore,
                    Build = build
                };

                environment.CurrentDirectory = Path.GetDirectoryName(options.SolutionFile)!;

                var selectedProjects = solutionExplorer.DiscoverAndSelectProjects
                (
                    options.SolutionFile,
                    "[green]Select the projects to standardize:[/]",
                    "[yellow]No .csproj files found in the solution file.[/]"
                );

                if (selectedProjects.Count == 0)
                    return;

                standardizer.StandardizeVersions(options, [.. selectedProjects]);
            });
    }
}
