using System.Diagnostics;

namespace NetTools.Services;

/// <inheritdoc/>
public sealed class ProcessRunner : IProcessRunner
{
    /// <inheritdoc/>
    public int Run
    (
        ProcessStartInfo startInfo,
        DataReceivedEventHandler? outputDataReceived,
        DataReceivedEventHandler? errorDataReceived
    )
    {
        using var process = new Process();
        process.StartInfo = startInfo;

        if (outputDataReceived != null)
            process.OutputDataReceived += outputDataReceived;

        if (errorDataReceived != null)
            process.ErrorDataReceived += errorDataReceived;

        process.Start();

        if (outputDataReceived != null)
            process.BeginOutputReadLine();

        if (errorDataReceived != null)
            process.BeginErrorReadLine();

        process.WaitForExit();

        return process.ExitCode;
    }
}
