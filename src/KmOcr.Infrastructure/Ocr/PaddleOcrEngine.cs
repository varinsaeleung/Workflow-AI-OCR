using KmOcr.Application.Contracts.Ocr;
using Microsoft.Extensions.Options;

namespace KmOcr.Infrastructure.Ocr;

/// <summary>
/// OCR engine adapter that invokes PaddleOCR through a Python process.
/// </summary>
public sealed class PaddleOcrEngine : IOcrEngine
{
    private readonly PaddleOcrOptions _options;
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Creates the PaddleOCR engine with options and a process runner.
    /// </summary>
    public PaddleOcrEngine(IOptions<PaddleOcrOptions> options, IProcessRunner processRunner)
    {
        _options = options.Value;
        _processRunner = processRunner;
    }

    /// <summary>
    /// Runs PaddleOCR for the given file and parses its JSON output.
    /// </summary>
    public async Task<OcrEngineResult> ExtractAsync(string filePath, string contentType, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("OCR input file was not found.", filePath);
        }

        var scriptPath = ResolveScriptPath();
        var arguments = new[]
        {
            scriptPath,
            "--input",
            filePath,
            "--content-type",
            contentType,
            "--languages",
            _options.Languages
        };
        var result = await _processRunner.RunAsync(
            _options.PythonExecutable,
            arguments,
            TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)),
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"PaddleOCR failed with exit code {result.ExitCode}: {result.StandardError}");
        }

        return PaddleOcrJsonParser.Parse(result.StandardOutput);
    }

    /// <summary>
    /// Resolves the configured script path relative to the application base directory when needed.
    /// </summary>
    private string ResolveScriptPath()
    {
        if (Path.IsPathFullyQualified(_options.ScriptPath))
        {
            return _options.ScriptPath;
        }

        return Path.Combine(AppContext.BaseDirectory, _options.ScriptPath);
    }
}
