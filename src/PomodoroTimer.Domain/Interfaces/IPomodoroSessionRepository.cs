using PomodoroTimer.Domain.Entities;

namespace PomodoroTimer.Domain.Interfaces;

public interface IPomodoroSessionRepository : IRepository<PomodoroSession>
{
    Task<IEnumerable<PomodoroSession>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PomodoroSession>> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default);
}
