namespace PomodoroTimer.Domain.Interfaces;

public interface ISessionStorage
{
    Task SaveSessionAsync(int userId, CancellationToken cancellationToken = default);
    Task<int?> GetSavedUserIdAsync(CancellationToken cancellationToken = default);
    Task ClearSessionAsync(CancellationToken cancellationToken = default);
}
