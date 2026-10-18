namespace PomodoroTimer.Domain.Interfaces;

public interface IPasswordHasher
{
    string GenerateSalt();
    string HashPassword(string password, string salt);
    bool VerifyPassword(string password, string hash, string salt);
}
