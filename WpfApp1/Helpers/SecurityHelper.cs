using System.Security.Cryptography;

namespace RalseiWarehouse.Helpers;

/// <summary>Contains the deliberately centralized password-hashing implementation.</summary>
public static class SecurityHelper
{
    private const int Iterations = 210_000;

    /// <summary>Hashes a password with PBKDF2-SHA256 and a random salt.</summary>
    /// <param name="password">The clear-text password.</param>
    /// <returns>A versioned value suitable for database storage.</returns>
    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>Verifies a password using a fixed-time hash comparison.</summary>
    /// <param name="password">The supplied clear-text password.</param>
    /// <param name="stored">The stored versioned hash.</param>
    /// <returns>Whether the password is valid.</returns>
    public static bool VerifyPassword(string password, string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return false;
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2-SHA256" || !int.TryParse(parts[1], out var iterations)) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]); var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }
}
