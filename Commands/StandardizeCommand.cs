using System.CommandLine;
using NetTools.Services;

namespace NetTools.Commands;

/// <summary>
/// Command to standardize NuGet package versions.
/// </summary>
public sealed class StandardizeCommand : Command
{
    private readonly NugetVersionStandardizer _standardizer;
    private readonly SolutionExplorer _solutionExplorer;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardizeCommand"/> class.
    /// </summary>
    public StandardizeCommand() : base("st", "Standardize NuGet package versions in a solution.")
    {
        _standardizer = new NugetVersionStandardizer();
        _solutionExplorer = new SolutionExplorer();

        var solutionFileArgument = new Option<string>
        (
            aliases: ["--sln", "-s"],
            description: "The path to the .sln file to discover projects."
        );

        var verboseOption = new Option<bool>(["--verbose", "-v"], () => false, "Show detailed output of dotnet commands.");
        var cleanOption = new Option<bool>(["--clean", "-c"], () => false, "Clean the solution after standardization.");
        var restoreOption = new Option<bool>(["--restore", "-r"], () => false, "Restore the solution after standardization.");
        var buildOption = new Option<bool>(["--build", "-b"], () => false, "Build the solution after standardization.");

        AddOption(verboseOption);
        AddOption(solutionFileArgument);
        AddOption(cleanOption);
        AddOption(restoreOption);
        AddOption(buildOption);

        this.SetHandler
        (
            (solutionFile, verbose, clean, restore, build) =>
            {
                var options = new StandardizeCommandOptions
                {
                    SolutionFile = solutionFile,
                    Verbose = verbose,
                    Clean = clean,
                    Restore = restore,
                    Build = build
                };
                var selectedProjects = _solutionExplorer.DiscoverProjects(solutionFile);

                _standardizer.StandardizeVersions(options, [..selectedProjects]);
            },
            solutionFileArgument, verboseOption, cleanOption, restoreOption, buildOption
        );
    }
}
