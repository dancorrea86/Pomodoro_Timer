using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Models;

namespace PomodoroTimer.Domain.Interfaces;

public interface IAuthService
{
    User? CurrentUser { get; }
    bool IsAuthenticated { get; }

    Task<AuthResult> RegisterAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<User?> TryRestoreSessionAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
}
