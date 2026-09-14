namespace KmOcr.Infrastructure.Ai;

/// <summary>
/// Configuration values used by the Ollama AI provider.
/// </summary>
public sealed class OllamaAiOptions
{
    /// <summary>
    /// Gets or sets the base URL of the Ollama server.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Gets or sets the Ollama model name used for extraction.
    /// </summary>
    public string Model { get; set; } = "llama3.1";

    /// <summary>
    /// Gets or sets the HTTP timeout in seconds for AI requests.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}
