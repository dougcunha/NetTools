using NetTools.Commands;
using NetTools.Helpers;
using Spectre.Console;

namespace NetTools.Services;

/// <summary>
/// Service responsible for standardizing NuGet package versions in a solution.
/// </summary>
public sealed class NugetVersionStandardizer
(
    IAnsiConsole console,
    IDotnetCommandRunner dotnetRunner,
    ICsprojHelpers csprojHelpers
) : INugetVersionStandardizer
{    /// <summary>
    /// Standardizes the NuGet package versions in the given solution directory by updating .csproj files.
    /// </summary>
    /// <param name="options">
    /// A <see cref="StandardizeCommandOptions"/> instance containing options for the command.
    /// </param>
    /// <param name="projectPaths">The paths to the solutions files.</param>
    public void StandardizeVersions(StandardizeCommandOptions options, params string[] projectPaths)
    {
        if (string.IsNullOrWhiteSpace(options.SolutionFile))
        {
            console.MarkupLine("[red]Solution file path cannot be null or empty.[/]");

            return;
        }

        var solutionPath = Path.GetDirectoryName(options.SolutionFile)!;

        var (multiVersionPackages, projectPackageMap) = DiscoverPackagesWithMultipleVersions(solutionPath, projectPaths);

        if (multiVersionPackages.Count == 0)
        {
            console.MarkupLine("[green]No packages with multiple versions found.[/]");

            return;
        }        var choices = multiVersionPackages.Select(static kvp => $"{kvp.Key} ({string.Join(", ", kvp.Value.Order())})").ToList();

        var selected = console.Prompt
        (
            new MultiSelectionPrompt<string>()
                .Title("[yellow]Select the packages with multiple versions to standardize:[/]")
                .NotRequired()
                .PageSize(20)
                .MoreChoicesText("[grey](Use space to select, enter to confirm)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to select, [green]<enter>[/] to confirm)[/]")
                .AddChoiceGroup("Select all", choices.Order())
        );

        if (selected.Count == 0)
        {
            console.MarkupLine("[yellow]No package selected.[/]");

            return;
        }

        StandardizeSelectedPackages(multiVersionPackages, projectPackageMap, selected);

        if (dotnetRunner.RunSequentialCommands
            (
                solutionPath,
                Path.GetFileName(options.SolutionFile),
                options.Verbose,
                clean: options.Clean,
                restore: options.Restore,
                build: options.Build
            )
        )
        {
            console.MarkupLine("[green]NuGet package versions standardized and solution cleaned/restored successfully.[/]");
        }
    }

    /// <summary>
    /// Discovers NuGet packages with multiple versions and returns the data for further processing.
    /// </summary>
    /// <param name="solutionDirectory">The solution directory.</param>
    /// <param name="projectPaths">Relative paths of the projects.</param>
    /// <returns>Tuple with multi-version packages and project-package map.</returns>
    private (Dictionary<string, HashSet<string>> multiVersionPackages, Dictionary<string, Dictionary<string, string>> projectPackageMap) DiscoverPackagesWithMultipleVersions
    (
        string solutionDirectory,
        params string[] projectPaths
    )
    {
        var packageVersions = new Dictionary<string, HashSet<string>>();
        var projectPackageMap = new Dictionary<string, Dictionary<string, string>>();

        foreach (var relativePath in projectPaths)
        {
            var csprojPath = Path.Combine(solutionDirectory, relativePath);
            var pkgs = csprojHelpers.GetPackagesFromCsproj(csprojPath);
            projectPackageMap[csprojPath] = pkgs;

            foreach (var (id, version) in pkgs)
            {
                if (!packageVersions.TryGetValue(id, out var value))
                {
                    value = [];
                    packageVersions[id] = value;
                }

                value.Add(version);
            }
        }

        var multiVersionPackages = packageVersions
            .Where(static kvp => kvp.Value.Count > 1)
            .ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value);

        return (multiVersionPackages, projectPackageMap);
    }

    /// <summary>
    /// Standardizes the selected packages to the highest version in all projects.
    /// </summary>
    /// <param name="multiVersionPackages">Packages with multiple versions.</param>
    /// <param name="projectPackageMap">Map of project to its packages.</param>
    /// <param name="selected">Selected package display names.</param>
    private void StandardizeSelectedPackages(Dictionary<string, HashSet<string>> multiVersionPackages, Dictionary<string, Dictionary<string, string>> projectPackageMap, List<string> selected)
    {
        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Standardizing package versions...", _ =>
            {
                foreach (var selectedDisplay in selected)
                {
                    var packageId = selectedDisplay.Split(' ')[0];

                    var maxVersion = multiVersionPackages[packageId].OrderByDescending(static v => v, StringComparer.OrdinalIgnoreCase)
                        .First(static v => !v.Equals("Unknown", StringComparison.OrdinalIgnoreCase));

                    foreach (var (csprojPath, pkgs) in projectPackageMap)
                    {
                        if (!pkgs.TryGetValue(packageId, out var currentVersion) || currentVersion == maxVersion)
                            continue;

                        csprojHelpers.UpdatePackageVersionInCsproj(csprojPath, packageId, maxVersion);
                        console.MarkupLine($"[green]Updated » {packageId} in {Path.GetFileName(csprojPath)} to version {maxVersion}.[/]");
                    }
                }
            });
    }
}
