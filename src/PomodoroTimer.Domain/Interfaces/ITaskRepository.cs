using PomodoroTimer.Domain.Entities;

namespace PomodoroTimer.Domain.Interfaces;

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId, bool? isCompleted = null, CancellationToken cancellationToken = default);
}
