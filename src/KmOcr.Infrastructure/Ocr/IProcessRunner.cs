namespace KmOcr.Infrastructure.Ocr;

/// <summary>
/// Runs external processes for infrastructure adapters.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a process and returns its captured output.
    /// </summary>
    Task<ProcessRunResult> RunAsync(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken);
}

/// <summary>
/// Captured output from an external process execution.
/// </summary>
public sealed record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);
