using System.Diagnostics;

namespace KmOcr.Infrastructure.Ocr;

/// <summary>
/// Executes external processes with stdout, stderr, and timeout handling.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    /// <summary>
    /// Starts a process, waits for completion, and returns the captured output.
    /// </summary>
    public async Task<ProcessRunResult> RunAsync(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start process '{fileName}'.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutSource.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            KillProcess(process);
            throw new TimeoutException($"Process '{fileName}' exceeded timeout {timeout.TotalSeconds:N0} seconds.");
        }

        return new ProcessRunResult(process.ExitCode, await stdoutTask, await stderrTask);
    }

    /// <summary>
    /// Kills a process tree when a timeout occurs.
    /// </summary>
    private static void KillProcess(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }
}
