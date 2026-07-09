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
        var v1 = ParseVersion(version1);
        var v2 = ParseVersion(version2);

        return v1 > v2 ? version1 : version2;
    }

    /// <summary>
    /// Determines whether a NuGet version is a prerelease version.
    /// </summary>
    /// <param name="version">The version to check.</param>
    /// <returns>
    /// True if the version is a prerelease version; otherwise, false.
    /// </returns>
    public static bool IsPrerelease(string version) => ParseVersion(version).IsPrerelease;

    /// <summary>
    /// Parses a NuGet version string, supporting both plain versions (e.g. "1.2.3") and fixed
    /// version range notation (e.g. "[1.2.3]") used to pin an exact package version.
    /// </summary>
    /// <param name="version">The version string to parse.</param>
    /// <returns>The parsed <see cref="NuGetVersion"/>.</returns>
    private static NuGetVersion ParseVersion(string version)
    {
        if (NuGetVersion.TryParse(version, out var parsedVersion))
            return parsedVersion;

        var range = VersionRange.Parse(version);

        return range.MinVersion ?? throw new ArgumentException($"'{version}' is not a valid version string.", nameof(version));
    }
}
