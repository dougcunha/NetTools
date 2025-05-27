using System.IO.Abstractions;
using System.Xml;
using System.Xml.Linq;
using NetTools.Models;
using NetTools.Services;
using Spectre.Console;

namespace NetTools.Helpers;

public sealed class CsprojHelpers(IFileSystem fileSystem, IXmlService xmlService, IAnsiConsole console)
{
    private static readonly XmlWriterSettings _xmlWriterSettings = new()
    {
        Indent = true,
        OmitXmlDeclaration = true,
        Encoding = new System.Text.UTF8Encoding(false)
    };

    /// <summary>
    /// Parses a .csproj file and collects NuGet package ids and their versions.
    /// </summary>
    /// <param name="csprojPath">The path to the .csproj file.</param>
    /// <returns>A dictionary with package id as key and version as value.</returns>
    public Dictionary<string, string> GetPackagesFromCsproj(string csprojPath)
    {
        var packages = new Dictionary<string, string>();

        if (!fileSystem.File.Exists(csprojPath))
            return packages;

        var doc = xmlService.Load(csprojPath);
        var packageRefs = doc.Descendants().Where(e => e.Name.LocalName == "PackageReference");

        foreach (var pr in packageRefs)
        {
            var id = pr.Attribute("Include")?.Value;
            var version = pr.Attribute("Version")?.Value ?? pr.Element("Version")?.Value;

            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(version))
                packages[id] = version;
        }

        return packages;
    }

    /// <summary>
    /// Gets all packages from the selected projects and maps them to their versions.
    /// </summary>
    /// <param name="projects">
    /// The list of project paths to scan for packages.
    /// </param>
    /// <returns>
    /// A dictionary containing all unique package IDs and their versions across the projects.
    /// </returns>
    public Dictionary<string, string> RetrieveUniquePackageVersions(Dictionary<string, List<Package>> projectsPackages)
    {
        Dictionary<string, string> allPackages = [];

        foreach (var (project, packages) in projectsPackages)
        {
            foreach (var pkg in packages)
            {
                // Compare the versions and keep the latest one
                allPackages[pkg.Id] = allPackages.TryGetValue(pkg.Id, out var value)
                    ? NugetVersionComparer.GetGreaterVersion(value, pkg.Version) 
                    : pkg.Version;
            }
        }

        return allPackages;
    }

    /// <summary>
    /// Gets packages from multiple projects and returns them as a dictionary.
    /// Each key is a project path and the value is a list of Package objects containing the ID and version.
    /// </summary>
    /// <param name="projects">
    /// A list of project paths to scan for packages.
    /// </param>
    /// <returns>
    /// A dictionary where each key is a project path and the value is a list of Package objects.
    /// </returns>
    public Dictionary<string, List<Package>> GetPackagesFromProjects(params List<string> projects)
    {
        var packages = new Dictionary<string, List<Package>>();

        foreach (var project in projects)
        {
            var projectPackages = GetPackagesFromCsproj(project);

            foreach (var (id, version) in projectPackages)
            {
                if (!packages.ContainsKey(project))
                    packages[project] = [];

                packages[project].Add(new Package(id, version));
            }
        }

        return packages;
    }

    /// <summary>
    /// Gets a list of outdated packages by comparing installed versions with the latest available versions.
    /// </summary>
    /// <param name="allPackages">
    /// The dictionary containing all packages with their installed versions.
    /// </param>
    /// <param name="latestVersions">
    /// The dictionary containing the latest available versions of the packages.
    /// </param>
    /// <param name="includePrerelease">
    /// Determines whether to include prerelease versions in the search for updates.
    /// </param>
    /// <returns>
    /// A list of tuples where each tuple contains the package ID, installed version, and latest version.
    /// </returns>
    public List<(string PackageId, string Installed, string? Latest)> GetOutdatedPackages
    (
        Dictionary<string, string> allPackages,
        Dictionary<string, string?> latestVersions,
        bool includePrerelease = false
    )
    {
        List<(string PackageId, string Installed, string? Latest)> outdated = [];

        foreach (var (pkgId, installedVersion) in allPackages)
        {
            if (NugetVersionComparer.IsPrerelease(installedVersion) && !includePrerelease)
                continue; // Skip prerelease versions if not included

            var latest = latestVersions[pkgId];

            if (string.IsNullOrEmpty(latest))
                continue;

            var greaterVersion = NugetVersionComparer.GetGreaterVersion(installedVersion, latest);

            // If the installed version is not the latest, add to the list
            if (greaterVersion != installedVersion)
                outdated.Add((pkgId, installedVersion, greaterVersion));
        }

        return outdated;
    }

    /// <summary>
    /// Updates the selected packages in their respective projects.
    /// </summary>
    /// <param name="projectPackages">
    /// The dictionary containing project paths and their packages with versions.
    /// </param>
    /// <param name="latestVersions">
    /// The dictionary containing the latest available versions of the packages.
    /// </param>
    /// <param name="selected">
    /// The list of selected packages to update, containing tuples of package name and ID.
    /// </param>
    public void UpdatePackagesInProjects
    (
        Dictionary<string, List<Package>> projectPackages,
        Dictionary<string, string?> latestVersions,
        List<(string, string)> selected
    )
    {
        foreach (var (_, pkgId) in selected)
        {
            foreach (var (projectPath, packages) in projectPackages)
            {
                var pkg = packages.FirstOrDefault(p => p.Id == pkgId);

                if (pkg == default)
                    continue;

                var newVersion = latestVersions[pkgId];

                if (string.IsNullOrEmpty(newVersion))
                {
                    console.MarkupLine($"[red]No latest version found for package '{pkgId}' in project '{projectPath}'.[/]");

                    continue;
                }

                UpdatePackageVersionInCsproj(projectPath, pkgId, newVersion);
            }
        }
    }

    /// <summary>
    /// Removes a NuGet package from a .csproj file.
    /// </summary>
    /// <param name="csprojPath">The path to the .csproj file.</param>
    /// <param name="packageId">The NuGet package id.</param>
    public void RemovePackageFromCsproj(string csprojPath, string packageId)
    {
        var doc = xmlService.Load(csprojPath, LoadOptions.PreserveWhitespace);

        var packageRefs = doc.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference")
            .ToList();

        var removed = false;

        foreach (var pr in packageRefs)
        {
            if (!string.Equals(pr.Attribute("Include")?.Value, packageId, StringComparison.OrdinalIgnoreCase))
                continue;

            pr.Remove();
            removed = true;
        }

        if (removed)
            xmlService.WriteTo(csprojPath, doc.ToString(), _xmlWriterSettings);
    }


    /// <summary>
    /// Checks if a .csproj contains a given NuGet package (any version).
    /// </summary>
    /// <param name="csprojPath">The path to the .csproj file.</param>
    /// <param name="packageId">The NuGet package id.</param>
    /// <returns>True if the package exists, false otherwise.</returns>
    public bool HasPackage(string csprojPath, string packageId)
    {
        var doc = xmlService.Load(csprojPath, LoadOptions.PreserveWhitespace);

        return doc.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference")
            .Any(pr => string.Equals(pr.Attribute("Include")?.Value, packageId, StringComparison.OrdinalIgnoreCase));
    }
    
    
    /// <summary>
    /// Updates the version of a NuGet package in a .csproj file.
    /// </summary>
    /// <param name="csprojPath">The path to the .csproj file.</param>
    /// <param name="packageId">The package id.</param>
    /// <param name="newVersion">The new version to set.</param>
    public void UpdatePackageVersionInCsproj(string csprojPath, string packageId, string newVersion)
    {
        var doc = xmlService.Load(csprojPath, LoadOptions.PreserveWhitespace);
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
            xmlService.WriteTo(csprojPath, doc.ToString(), _xmlWriterSettings);
    }
}
