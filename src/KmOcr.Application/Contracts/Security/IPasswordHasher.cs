namespace KmOcr.Application.Contracts.Security;

/// <summary>
/// Provides password hashing and verification.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password for storage.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored password hash.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}
