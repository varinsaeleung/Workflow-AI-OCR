using KmOcr.Domain.Common;

namespace KmOcr.Domain.Documents;

/// <summary>
/// Stores one metadata key and value for a document.
/// </summary>
public sealed class DocumentMetadata : Entity
{
    /// <summary>
    /// Creates an empty metadata row for Entity Framework.
    /// </summary>
    private DocumentMetadata()
    {
        Key = string.Empty;
        Value = string.Empty;
    }

    /// <summary>
    /// Creates a metadata row for a document.
    /// </summary>
    public DocumentMetadata(Guid documentId, string key, string value)
    {
        DocumentId = documentId;
        Key = RequireText(key, nameof(key));
        Value = RequireText(value, nameof(value));
    }

    /// <summary>
    /// Gets the owning document identifier.
    /// </summary>
    public Guid DocumentId { get; private set; }

    /// <summary>
    /// Gets the metadata key.
    /// </summary>
    public string Key { get; private set; }

    /// <summary>
    /// Gets the metadata value.
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Replaces the metadata value.
    /// </summary>
    public void UpdateValue(string value)
    {
        Value = RequireText(value, nameof(value));
        Touch();
    }

    /// <summary>
    /// Validates required text and returns the trimmed value.
    /// </summary>
    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
