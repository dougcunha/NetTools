namespace NetTools.Services;

/// <summary>
/// Interface for environment-related services.
/// </summary>
public interface IEnvironmentService
{
    /// <summary>
    /// Gets or sets the current working directory.
    /// </summary>
    string CurrentDirectory { get; set; }
}
