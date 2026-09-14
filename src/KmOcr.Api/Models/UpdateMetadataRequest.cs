namespace KmOcr.Api.Models;

/// <summary>
/// HTTP request body used to update document metadata.
/// </summary>
public sealed class UpdateMetadataRequest
{
    /// <summary>
    /// Gets or sets the actor performing the metadata change.
    /// </summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets metadata key-value pairs.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}
