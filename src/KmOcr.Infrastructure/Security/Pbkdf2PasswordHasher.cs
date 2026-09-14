using System.Security.Cryptography;
using KmOcr.Application.Contracts.Security;

namespace KmOcr.Infrastructure.Security;

/// <summary>
/// Hashes passwords using PBKDF2 with SHA-256 and per-password random salts.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Marker = "PBKDF2";
    private const string Version = "V1";
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;
    private const int Iterations = 210_000;

    /// <summary>
    /// Hashes a plaintext password for storage.
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var key = DeriveKey(password, salt, Iterations);
        return string.Join('$', Marker, Version, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    /// <summary>
    /// Verifies a plaintext password against a stored password hash.
    /// </summary>
    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        if (!TryDecodeHash(passwordHash, out var iterations, out var salt, out var expectedKey))
        {
            return false;
        }

        var actualKey = DeriveKey(password, salt, iterations);
        return actualKey.Length == expectedKey.Length && CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }

    /// <summary>
    /// Derives a fixed-size key from the supplied password and salt.
    /// </summary>
    private static byte[] DeriveKey(string password, byte[] salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeySizeBytes);
    }

    /// <summary>
    /// Parses the stored hash format into its cryptographic components.
    /// </summary>
    private static bool TryDecodeHash(string passwordHash, out int iterations, out byte[] salt, out byte[] key)
    {
        iterations = 0;
        salt = [];
        key = [];
        var parts = passwordHash.Split('$');

        if (parts.Length != 5 || parts[0] != Marker || parts[1] != Version || !int.TryParse(parts[2], out iterations))
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[3]);
            key = Convert.FromBase64String(parts[4]);
            return salt.Length == SaltSizeBytes && key.Length == KeySizeBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
