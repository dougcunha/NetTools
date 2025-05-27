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
