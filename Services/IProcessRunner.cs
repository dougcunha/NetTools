using System.Diagnostics;

namespace NetTools.Services;

/// <summary>
/// Interface for running system processes.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a process with the specified start info and waits for it to exit.
    /// </summary>
    /// <param name="startInfo">The process start information.</param>
    /// <returns>The exit code of the process.</returns>
    int Run
    (
        ProcessStartInfo startInfo,
        DataReceivedEventHandler? outputDataReceived,
        DataReceivedEventHandler? errorDataReceived
    );
}
