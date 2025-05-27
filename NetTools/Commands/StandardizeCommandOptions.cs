namespace NetTools.Commands;

/// <summary>
/// Options for the Standardize command.
/// </summary>
public sealed class StandardizeCommandOptions
{
    /// <summary>
    /// The path to the .sln file to discover projects.
    /// </summary>
    public string? SolutionFile { get; init; }

    /// <summary>
    /// If true, shows detailed output of dotnet commands.
    /// </summary>
    public bool Verbose { get; init; }

    /// <summary>
    /// If true, runs 'dotnet clean' after standardization.
    /// </summary>
    public bool Clean { get; init; }

    /// <summary>
    /// If true, runs 'dotnet restore' after standardization.
    /// </summary>
    public bool Restore { get; init; }

    /// <summary>
    /// If true, runs 'dotnet build' after standardization.
    /// </summary>
    public bool Build { get; init; }
}
