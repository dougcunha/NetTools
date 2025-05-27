namespace NetTools.Services;

/// <summary>
/// Service for interacting with NuGet API.
/// </summary>
public interface INugetService
{
    /// <summary>
    /// Gets the latest version of a NuGet package by its ID.
    /// </summary>
    /// <param name="packageId">The NuGet package ID.</param>
    /// <param name="includePrerelease">If true, includes prerelease versions; otherwise, only stable versions are considered.</param>
    /// <returns>The latest version string, or null if not found.</returns>
    Task<string?> GetLatestVersionAsync(string packageId, bool includePrerelease = false);
}
