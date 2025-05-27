namespace NetTools.Models;

/// <summary>
/// Represents a NuGet package with its ID and version.
/// </summary>
/// <param name="Id">The unique identifier of the package.</param>
/// <param name="Version">The version of the package.</param>
public record class Package(string Id, string Version);
