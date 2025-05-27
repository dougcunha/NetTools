namespace NetTools.Services;

/// <summary>
/// Interface for running dotnet CLI commands in the solution context.
/// </summary>
public interface IDotnetCommandRunner
{
    /// <summary>
    /// Runs a sequence of dotnet CLI commands based on the provided options.
    /// </summary>
    /// <param name="solutionDirectory">The path to the solution directory.</param>
    /// <param name="solutionFile">The solution or project file to target (optional).</param>
    /// <param name="verbose">If true, enables verbose output.</param>
    /// <param name="clean">If true, runs 'dotnet clean'.</param>
    /// <param name="restore">If true, runs 'dotnet restore'.</param>
    /// <param name="build">If true, runs 'dotnet build'.</param>
    /// <remarks>
    /// This method runs the commands in the order: clean, restore, build.
    /// If any command fails, the subsequent commands are not executed.
    /// </remarks>
    bool RunSequentialCommands
    (
        string solutionDirectory,
        string? solutionFile = null,
        bool verbose = false,
        bool clean = false,
        bool restore = false,
        bool build = false
    );
}
