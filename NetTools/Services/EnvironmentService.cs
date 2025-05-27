using System.Diagnostics.CodeAnalysis;

namespace NetTools.Services;

/// <summary>
/// Wrapper for environment-related operations, such as managing the current directory.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EnvironmentService : IEnvironmentService
{
    /// <inheritdoc />
    public string CurrentDirectory
    {
        get => Environment.CurrentDirectory;
        set => Environment.CurrentDirectory = value;
    }
}
