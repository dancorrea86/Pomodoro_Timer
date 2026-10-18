using System.Security.Cryptography;
using PomodoroTimer.Domain.Interfaces;

namespace PomodoroTimer.Services.Security;

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string GenerateSalt()
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        return Convert.ToBase64String(saltBytes);
    }

    public string HashPassword(string password, string salt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(salt);

        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            Algorithm,
            HashSizeBytes);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
        {
            return false;
        }

        try
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] storedHashBytes = Convert.FromBase64String(hash);

            byte[] computedHashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                Iterations,
                Algorithm,
                storedHashBytes.Length);

            return CryptographicOperations.FixedTimeEquals(computedHashBytes, storedHashBytes);
        }
        catch
        {
            return false;
        }
    }
}
