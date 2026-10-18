using PomodoroTimer.Domain.Entities;

namespace PomodoroTimer.Domain.Models;

public class AuthResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public User? User { get; }

    private AuthResult(bool success, User? user, string? errorMessage)
    {
        Success = success;
        User = user;
        ErrorMessage = errorMessage;
    }

    public static AuthResult Ok(User user) => new(true, user, null);

    public static AuthResult Fail(string errorMessage) => new(false, null, errorMessage);
}
