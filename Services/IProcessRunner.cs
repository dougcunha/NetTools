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
    /// <param name="outputDataReceived">
    /// A handler for the standard output data received event.
    /// </param>
    /// <param name="errorDataReceived">
    /// A handler for the standard error data received event.
    /// </param>
    /// <returns>The exit code of the process.</returns>
    int Run
    (
        ProcessStartInfo startInfo,
        DataReceivedEventHandler? outputDataReceived,
        DataReceivedEventHandler? errorDataReceived
    );
}
