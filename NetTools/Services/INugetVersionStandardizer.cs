using NetTools.Commands;

namespace NetTools.Services;

/// <summary>
/// Interface for NuGet version standardization service.
/// </summary>
public interface INugetVersionStandardizer
{
    /// <summary>
    /// Standardizes the NuGet package versions in the given solution directory by updating .csproj files.
    /// </summary>
    /// <param name="options">
    /// A <see cref="StandardizeCommandOptions"/> instance containing options for the command.
    /// </param>
    /// <param name="projectPaths">The paths to the solutions files.</param>
    void StandardizeVersions(StandardizeCommandOptions options, params string[] projectPaths);
}
