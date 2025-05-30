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
        var solutionFileArgument = new Argument<string?>
        (
            "solutionFile",
            static () => null,
            "The path to the .sln file to discover projects (optional). If omitted, the tool will search for a solution file in the current directory or prompt for selection."
        );

        var cleanOption = new Option<bool>(["--clean", "-c"], static () => false, "Clean the solution after standardization.");
        var restoreOption = new Option<bool>(["--restore", "-r"], static () => false, "Restore the solution after standardization.");
        var buildOption = new Option<bool>(["--build", "-b"], static () => false, "Build the solution after standardization.");
        var verboseOption = new Option<bool>(["--verbose", "-v"], static () => false, "Show detailed output of dotnet commands.");

        AddArgument(solutionFileArgument);
        AddOption(cleanOption);
        AddOption(restoreOption);
        AddOption(buildOption);
        AddOption(verboseOption);

        this.SetHandler
        (
            (solutionFile, verbose, clean, restore, build) =>
            {
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
            },
            solutionFileArgument,
            verboseOption,
            cleanOption,
            restoreOption,
            buildOption
        );
    }
}
