using NuGet.Versioning;

namespace NetTools.Helpers;

public static class NugetVersionComparer
{
    /// <summary>
    /// Compares two NuGet versions and returns the greater one.
    /// </summary>
    /// <param name="version1">The first version.</param>
    /// <param name="version2">The second version.</param>
    /// <returns>The greater version.</returns>
    public static string GetGreaterVersion(string version1, string version2)
    {
        var v1 = NuGetVersion.Parse(version1);
        var v2 = NuGetVersion.Parse(version2);
        
        return v1 > v2 ? version1 : version2;
    }

    /// <summary>
    /// Determines whether a NuGet version is a prerelease version.
    /// </summary>
    /// <param name="version">The version to check.</param>
    /// <returns>
    /// True if the version is a prerelease version; otherwise, false.
    /// </returns>
    public static bool IsPrerelease(string version)
    {
        var parsedNuGetVersion = NuGetVersion.Parse(version);

        return parsedNuGetVersion.IsPrerelease;
    }
}
