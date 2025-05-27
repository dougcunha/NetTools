using NetTools.Models;

namespace NetTools.Helpers;

/// <summary>
/// Interface for helper methods to work with .csproj files and NuGet packages.
/// </summary>
public interface ICsprojHelpers
{
    /// <summary>
    /// Parses a .csproj file and collects NuGet package ids and their versions.
    /// </summary>
    /// <param name="csprojPath">The path to the .csproj file.</param>
    /// <returns>A dictionary with package id as key and version as value.</returns>
    Dictionary<string, string> GetPackagesFromCsproj(string csprojPath);

    /// <summary>
    /// Gets all packages from the selected projects and maps them to their versions.
    /// </summary>
    /// <param name="projectsPackages">
    /// The list of project paths to scan for packages.
    /// </param>
    /// <returns>
    /// A dictionary containing all unique package IDs and their versions across the projects.
    /// </returns>
    Dictionary<string, string> RetrieveUniquePackageVersions(Dictionary<string, List<Package>> projectsPackages);

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
    List<(string PackageId, string Installed, string? Latest)> GetOutdatedPackages
    (
        Dictionary<string, string> allPackages,
        Dictionary<string, string?> latestVersions,
        bool includePrerelease = false
    );

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
    Dictionary<string, List<Package>> GetPackagesFromProjects(params List<string> projects);

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
    void UpdatePackagesInProjects
    (
        Dictionary<string, List<Package>> projectPackages,
        Dictionary<string, string?> latestVersions,
        List<(string, string)> selected
    );

    /// <summary>
    /// Removes a NuGet package from a .csproj file.
    /// </summary>
    /// <param name="csprojPath">The path to the .csproj file.</param>
    /// <param name="packageId">The NuGet package id.</param>
    void RemovePackageFromCsproj(string csprojPath, string packageId);

    /// <summary>
    /// Checks if a .csproj contains a given NuGet package (any version).
    /// </summary>
    /// <param name="csprojPath">The path to the .csproj file.</param>
    /// <param name="packageId">The NuGet package id.</param>
    /// <returns>True if the package exists, false otherwise.</returns>
    bool HasPackage(string csprojPath, string packageId);

    /// <summary>
    /// Updates the version of a NuGet package in a .csproj file.
    /// </summary>
    /// <param name="csprojPath">The path to the .csproj file.</param>
    /// <param name="packageId">The package id.</param>
    /// <param name="newVersion">The new version to set.</param>
    void UpdatePackageVersionInCsproj(string csprojPath, string packageId, string newVersion);
}
