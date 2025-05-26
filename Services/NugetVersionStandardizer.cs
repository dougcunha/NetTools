using NetTools.Commands;
using Spectre.Console;
using System.IO.Abstractions;
using System.Xml;
using System.Xml.Linq;

namespace NetTools.Services;

/// <summary>
/// Service responsible for standardizing NuGet package versions in a solution.
/// </summary>
public sealed class NugetVersionStandardizer
(
    IFileSystem fileSystem,
    IAnsiConsole console,
    IXDocumentLoader xDocumentLoader,
    IXmlWriterWrapper xmlWriterWrapper,
    DotnetCommandRunner dotnetRunner
)
{
    private static readonly XmlWriterSettings _xmlWriterSettings = new()
    {
        Indent = true,
        OmitXmlDeclaration = true,
        Encoding = new System.Text.UTF8Encoding(false)
    };

    /// <summary>
    /// Standardizes the NuGet package versions in the given solution directory by updating .csproj files.
    /// </summary>
    /// <param name="solutionFilePath">The path to the solution directory.</param>
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
        }

        var choices = multiVersionPackages.Select(kvp => $"{kvp.Key} ({string.Join(", ", kvp.Value.OrderBy(v => v))})").ToList();

        var selected = console.Prompt
        (
            new MultiSelectionPrompt<string>()
                .Title("[yellow]Select the packages with multiple versions to standardize:[/]")
                .NotRequired()
                .PageSize(20)
                .MoreChoicesText("[grey](Use space to select, enter to confirm)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to select, [green]<enter>[/] to confirm)[/]")
                .AddChoiceGroup("Select all", choices.OrderBy(c => c))
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
            console.MarkupLine("[green]NuGet package versions standardized and solution cleaned/restored successfully.[/]");
    }

    /// <summary>
    /// Parses a .csproj file and collects NuGet package ids and their versions.
    /// </summary>
    /// <param name="csprojPath">The path to the .csproj file.</param>
    /// <returns>A dictionary with package id as key and version as value.</returns>
    private Dictionary<string, string> GetPackagesFromCsproj(string csprojPath)
    {
        var packages = new Dictionary<string, string>();

        if (!fileSystem.File.Exists(csprojPath))
        {
            return packages;
        }

        var doc = xDocumentLoader.Load(csprojPath);
        var packageRefs = doc.Descendants().Where(e => e.Name.LocalName == "PackageReference");

        foreach (var pr in packageRefs)
        {
            var id = pr.Attribute("Include")?.Value;
            var version = pr.Attribute("Version")?.Value ?? pr.Element("Version")?.Value;

            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(version))
            {
                packages[id] = version;
            }
        }

        return packages;
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
            var pkgs = GetPackagesFromCsproj(csprojPath);
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
            .Where(kvp => kvp.Value.Count > 1)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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
            .Start("Standardizing package versions...", ctx =>
            {
                foreach (var selectedDisplay in selected)
                {
                    var packageId = selectedDisplay.Split(' ')[0];

                    var maxVersion = multiVersionPackages[packageId].OrderByDescending(v => v, StringComparer.OrdinalIgnoreCase)
                        .Where(static v => !v.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                        .First();

                    foreach (var (csprojPath, pkgs) in projectPackageMap)
                    {
                        if (!pkgs.TryGetValue(packageId, out var currentVersion) || currentVersion == maxVersion)
                            continue;

                        UpdatePackageVersionInCsproj(csprojPath, packageId, maxVersion);
                        console.MarkupLine($"[green]Updated » {packageId} in {Path.GetFileName(csprojPath)} to version {maxVersion}.[/]");
                    }
                }
            });
    }

    /// <summary>
    /// Updates the version of a NuGet package in a .csproj file.
    /// </summary>
    /// <param name="csprojPath">The path to the .csproj file.</param>
    /// <param name="packageId">The package id.</param>
    /// <param name="newVersion">The new version to set.</param>
    private void UpdatePackageVersionInCsproj(string csprojPath, string packageId, string newVersion)
    {
        var doc = xDocumentLoader.Load(csprojPath, LoadOptions.PreserveWhitespace);
        var packageRefs = doc.Descendants().Where(e => e.Name.LocalName == "PackageReference");
        var updated = false;

        foreach (var pr in packageRefs)
        {
            var id = pr.Attribute("Include")?.Value;

            if (id == packageId)
            {
                if (pr.Attribute("Version") != null)
                {
                    pr.Attribute("Version")!.Value = newVersion;
                    updated = true;

                    continue;
                }

                if (pr.Element("Version") != null)
                {
                    pr.Element("Version")!.Value = newVersion;
                    updated = true;
                }
            }
        }

        if (updated)
            xmlWriterWrapper.WriteTo(csprojPath, doc.ToString(), _xmlWriterSettings);
    }

    /// <summary>
    /// Checks if a .csproj contains a given NuGet package (any version).
    /// </summary>
    /// <param name="csprojPath">The path to the .csproj file.</param>
    /// <param name="packageId">The NuGet package id.</param>
    /// <returns>True if the package exists, false otherwise.</returns>
    public bool HasPackage(string csprojPath, string packageId)
    {
        var doc = xDocumentLoader.Load(csprojPath, LoadOptions.PreserveWhitespace);

        return doc.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference")
            .Any(pr => string.Equals(pr.Attribute("Include")?.Value, packageId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Removes a NuGet package from a .csproj file.
    /// </summary>
    /// <param name="csprojPath">The path to the .csproj file.</param>
    /// <param name="packageId">The NuGet package id.</param>
    public void RemovePackageFromCsproj(string csprojPath, string packageId)
    {
        var doc = xDocumentLoader.Load(csprojPath, LoadOptions.PreserveWhitespace);
        var packageRefs = doc.Descendants().Where(e => e.Name.LocalName == "PackageReference").ToList();
        var removed = false;

        foreach (var pr in packageRefs)
        {
            if (string.Equals(pr.Attribute("Include")?.Value, packageId, StringComparison.OrdinalIgnoreCase))
            {
                pr.Remove();
                removed = true;
            }
        }

        if (removed)
            xmlWriterWrapper.WriteTo(csprojPath, doc.ToString(), _xmlWriterSettings);
    }
}
