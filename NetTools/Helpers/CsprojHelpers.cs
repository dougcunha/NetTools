using System.IO.Abstractions;
using System.Xml;
using System.Xml.Linq;
using NetTools.Models;
using NetTools.Services;
using Spectre.Console;

namespace NetTools.Helpers;

public sealed class CsprojHelpers(IFileSystem fileSystem, IXmlService xmlService, IAnsiConsole console) : ICsprojHelpers
{
    private static readonly XmlWriterSettings _xmlWriterSettings = new()
    {
        Indent = true,
        OmitXmlDeclaration = true,
        Encoding = new System.Text.UTF8Encoding(false)
    };

    /// <inheritdoc/>
    public Dictionary<string, string> GetPackagesFromCsproj(string csprojPath)
    {
        var packages = new Dictionary<string, string>();

        if (!fileSystem.File.Exists(csprojPath))
            return packages;

        var doc = xmlService.Load(csprojPath);

        foreach (var pr in doc.Descendants().Where(static e => e.Name.LocalName == "PackageReference"))
        {
            var id = pr.Attribute("Include")?.Value;
            var version = pr.Attribute("Version")?.Value ?? pr.Element("Version")?.Value;

            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(version))
                packages[id] = version;
        }

        return packages;
    }

    /// <inheritdoc/>
    public Dictionary<string, string> RetrieveUniquePackageVersions(Dictionary<string, List<Package>> projectsPackages)
    {
        Dictionary<string, string> allPackages = [];

        foreach (var (_, packages) in projectsPackages)
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

                if (pkg is null)
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

    /// <inheritdoc/>
    public void RemovePackageFromCsproj(string csprojPath, string packageId)
    {
        var doc = xmlService.Load(csprojPath, LoadOptions.PreserveWhitespace);

        var packageRefs = doc.Descendants()
            .Where(static e => e.Name.LocalName == "PackageReference")
            .ToList();

        var removed = false;

        foreach (var pr in packageRefs.Where(pr => string.Equals(pr.Attribute("Include")?.Value, packageId, StringComparison.OrdinalIgnoreCase)))
        {
            pr.Remove();
            removed = true;
        }

        if (removed)
            xmlService.WriteTo(csprojPath, doc.ToString(), _xmlWriterSettings);
    }

    /// <inheritdoc/>
    public bool HasPackage(string csprojPath, string packageId)
    {
        var doc = xmlService.Load(csprojPath, LoadOptions.PreserveWhitespace);

        return doc.Descendants()
            .Where(static e => e.Name.LocalName == "PackageReference")
            .Any(pr => string.Equals(pr.Attribute("Include")?.Value, packageId, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public void UpdatePackageVersionInCsproj(string csprojPath, string packageId, string newVersion)
    {
        var doc = xmlService.Load(csprojPath, LoadOptions.PreserveWhitespace);
        var updated = false;

        foreach (var pr in doc.Descendants().Where(static e => e.Name.LocalName == "PackageReference"))
        {
            var id = pr.Attribute("Include")?.Value;

            if (id != packageId)
                continue;

            if (pr.Attribute("Version") != null)
            {
                pr.Attribute("Version")!.Value = newVersion;
                updated = true;

                continue;
            }

            if (pr.Element("Version") == null)
                continue;

            pr.Element("Version")!.Value = newVersion;
            updated = true;
        }

        if (updated)
            xmlService.WriteTo(csprojPath, doc.ToString(), _xmlWriterSettings);
    }
}
