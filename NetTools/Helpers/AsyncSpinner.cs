using System.Diagnostics.CodeAnalysis;
using Spectre.Console;

namespace NetTools.Helpers;

/// <summary>
/// Class to show an asynchronous spinner in the console.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AsyncSpinner : IAsyncDisposable, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _task;

    /// <summary>
    /// Shows an asynchronous spinner in the console with the specified message.
    /// </summary>
    /// <param name="console">
    /// An instance of <see cref="IAnsiConsole"/> to display the spinner.
    /// </param>
    /// <param name="message">
    /// A message to display alongside the spinner.
    /// </param>
    /// <returns>
    /// An instance of <see cref="AsyncSpinner"/> that manages the spinner lifecycle.
    /// </returns>
    public static AsyncSpinner Show(IAnsiConsole console, string message)
        => new(console, message);

    private AsyncSpinner(IAnsiConsole console, string message)
    {
        var status = console.Status().Spinner(Spinner.Known.Circle);

        _task = status.Start<Task>
        (
            message,
            _ => Task.Delay(999999, cancellationToken: _cancellationTokenSource.Token)
        );
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        await CastAndDispose(_cancellationTokenSource).ConfigureAwait(false);
        await CastAndDispose(_task).ConfigureAwait(false);

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                resource.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _task.Dispose();
    }
}
