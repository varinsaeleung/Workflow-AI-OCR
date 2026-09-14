namespace KmOcr.Domain.Common;

/// <summary>
/// Base entity that provides an identity and audit timestamps for all aggregates.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Initializes a new entity with a generated identifier.
    /// </summary>
    protected Entity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the immutable entity identifier.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets the creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; protected set; }

    /// <summary>
    /// Gets the latest update timestamp in UTC when the entity has changed.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; protected set; }

    /// <summary>
    /// Marks the entity as changed at the current UTC time.
    /// </summary>
    public void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
