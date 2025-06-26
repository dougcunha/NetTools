using System.CommandLine;
using NetTools.Helpers;
using NetTools.Services;
using Spectre.Console;

namespace NetTools.Commands;

/// <summary>
/// Command to check for NuGet package updates in selected projects.
/// </summary>
public sealed class UpdateCommand : Command
{
    private readonly ISolutionExplorer _solutionExplorer;
    private readonly INugetService _nugetService;
    private readonly IAnsiConsole _console;
    private readonly ICsprojHelpers _csprojHelpers;
    private readonly IDotnetCommandRunner _dotnetRunner;
    private readonly IEnvironmentService _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCommand"/> class.
    /// </summary>
    public UpdateCommand
    (
        ISolutionExplorer solutionExplorer,
        INugetService nugetService,
        IAnsiConsole console,
        ICsprojHelpers csprojHelpers,
        IDotnetCommandRunner dotnetRunner,
        IEnvironmentService environment
    ) : base("upd", "Check for NuGet package updates in selected projects.")
    {
        _solutionExplorer = solutionExplorer;
        _nugetService = nugetService;
        _console = console;
        _csprojHelpers = csprojHelpers;
        _dotnetRunner = dotnetRunner;
        _environment = environment;

        var includePrereleaseArgument = new Option<bool>("--include-prerelease", "-p")
        {
            Description = "If true, includes prerelease versions when checking for updates."
        };

        var cleanOption = new Option<bool>("--clean", "-c")
        {
            Description = "Clean the solution after update."
        };

        var restoreOption = new Option<bool>("--restore", "-r")
        {
            Description = "Restore the solution after update."
        };

        var buildOption = new Option<bool>("--build", "-b")
        {
            Description = "Build the solution after update."
        };

        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Show detailed output of dotnet commands."
        };

        var solutionFileArgument = new Argument<string?>("solutionFile")
        {
            Description = "The path to the .sln file to discover projects (optional). If omitted, the tool will search for a solution file in the current directory or prompt for selection.",
            Arity = ArgumentArity.ZeroOrOne
        };

        Add(solutionFileArgument);
        Add(includePrereleaseArgument);
        Add(cleanOption);
        Add(restoreOption);
        Add(buildOption);
        Add(verboseOption);

        SetAction
        (
            result =>
            {
                var solutionFile = result.GetValue(solutionFileArgument);
                var includePrerelease = result.GetValue(includePrereleaseArgument);
                var clean = result.GetValue(cleanOption);
                var restore = result.GetValue(restoreOption);
                var build = result.GetValue(buildOption);
                var verbose = result.GetValue(verboseOption);

                return HandleAsync(solutionFile, includePrerelease, clean, restore, build, verbose);
            }
        );
    }

    /// <summary>
    /// Handles the command execution.
    /// </summary>
    /// <param name="solutionFile">
    /// The path to the solution file. If null or empty, the command will search for a solution file in the current directory or prompt the user to select one.
    /// </param>
    /// <param name="includePrerelease">
    /// Determines whether to include prerelease versions when checking for updates.
    /// </param>
    /// <param name="clean">
    /// Determines whether to clean the solution after checking for updates.
    /// </param>
    /// <param name="restore">
    /// Determines whether to restore the solution after checking for updates.
    /// </param>
    /// <param name="build">
    /// Determines whether to build the solution after checking for updates.
    /// </param>
    /// <param name="verbose">
    /// Determines whether to show detailed output of dotnet commands.
    /// </param>
    /// <returns>
    /// True if any packages were updated, false otherwise.
    /// </returns>
    private async Task<bool> HandleAsync
    (
        string? solutionFile,
        bool includePrerelease = false,
        bool clean = false,
        bool restore = false,
        bool build = false,
        bool verbose = false
    )
    {
        solutionFile = _solutionExplorer.GetOrPromptSolutionFile(solutionFile);
        _environment.CurrentDirectory = Path.GetDirectoryName(solutionFile)!;

        var projects = _solutionExplorer.DiscoverAndSelectProjects
        (
            solutionFile,
            "[green]Select the projects to check for updates:[/]",
            "[yellow]No .csproj files found in the solution file.[/]"
        );

        if (projects.Count == 0)
            return false;

        var projectPackages = _csprojHelpers.GetPackagesFromProjects(projects);
        var consolidatedPackages = _csprojHelpers.RetrieveUniquePackageVersions(projectPackages);
        var latestVersions = await FetchLatestPackageVersionsAsync(consolidatedPackages, includePrerelease).ConfigureAwait(false);
        var outdated = _csprojHelpers.GetOutdatedPackages(consolidatedPackages, latestVersions);

        if (outdated.Count == 0)
        {
            _console.MarkupLine("[green]All packages are up to date.[/]");

            return false;
        }

        var selected = await _console.PromptAsync
        (
            new MultiSelectionPrompt<(string Name, string Id)>()
                .Title("Select the packages to [green]update[/]:")
                .NotRequired()
                .PageSize(20)
                .MoreChoicesText("[grey](Move up and down to reveal more packages)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                .AddChoiceGroup(("Select all", ""), [.. outdated.Select(static e => ($"{e.PackageId} ({e.Installed} » {e.Latest})", e.PackageId))])
                .UseConverter(static e => e.Item1)
        ).ConfigureAwait(false);

        if (selected.Count == 0)
        {
            _console.MarkupLine("[yellow]No packages selected for update.[/]");

            return false;
        }

        _csprojHelpers.UpdatePackagesInProjects(projectPackages, latestVersions, selected);

        if  (_dotnetRunner.RunSequentialCommands
            (
                Path.GetDirectoryName(solutionFile)!,
                Path.GetFileName(solutionFile),
                verbose,
                clean,
                restore,
                build
            ))
        {
            _console.MarkupLine("[green]Selected packages updated successfully.[/]");
        }

        return true;
    }

    /// <summary>
    /// Asynchronously retrieves the latest available versions of the specified NuGet packages.
    /// </summary>
    /// <remarks>This method queries the NuGet service to fetch the latest versions of the provided packages.
    /// The progress of the operation is displayed in the console, including a progress bar and task details.</remarks>
    /// <param name="allPackages">A dictionary where the keys are the package IDs to query and the values are their current versions.</param>
    /// <param name="includePrerelease">
    /// Determines whether to include prerelease versions in the search for the latest package versions.
    /// </param>
    /// <returns>A dictionary where the keys are the package IDs and the values are the latest available versions. If a package's
    /// latest version cannot be determined, its value will be <see langword="null"/>.</returns>
    private async Task<Dictionary<string, string?>> FetchLatestPackageVersionsAsync(Dictionary<string, string> allPackages, bool includePrerelease)
    {
        var latestVersions = new Dictionary<string, string?>();

        await _console.Progress()
            .Columns
            (
                new ProgressBarColumn
                {
                    CompletedStyle = new Style(foreground: Color.Green1, decoration: Decoration.Conceal | Decoration.Bold | Decoration.Invert),
                    RemainingStyle = new Style(decoration: Decoration.Conceal),
                    FinishedStyle = new Style(foreground: Color.Green1, decoration: Decoration.Conceal | Decoration.Bold | Decoration.Invert)
                },
                new PercentageColumn(),
                new SpinnerColumn(),
                new ElapsedTimeColumn(),
                new TaskDescriptionColumn()
            )
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Querying NuGet for latest versions...", maxValue: allPackages.Count);

                foreach (var pkgId in allPackages.Keys)
                {
                    latestVersions[pkgId] = await _nugetService.GetLatestVersionAsync(pkgId, includePrerelease).ConfigureAwait(false);
                    task.Description = $"Checking {pkgId}...";

                    task.Increment(1);
                }

                task.StopTask();
                task.Description = "Completed checking for latest versions.";
            }).ConfigureAwait(false);

        return latestVersions;
    }
}
