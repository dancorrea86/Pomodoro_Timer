using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Enums;

namespace PomodoroTimer.Domain.Interfaces;

public interface ITaskService
{
    Task<TaskItem> CreateTaskAsync(int userId, string title, string? description = null, int estimatedPomodoros = 1, CancellationToken cancellationToken = default);
    Task<TaskItem?> GetTaskByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskItem>> GetTasksByUserIdAsync(int userId, bool? isCompleted = null, CancellationToken cancellationToken = default);
    Task<TaskItem> UpdateTaskAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task<TaskItem> UpdateTaskDetailsAsync(int taskId, string title, string? description, int estimatedPomodoros, CancellationToken cancellationToken = default);
    Task<TaskItem> CompleteTaskAsync(int taskId, bool isCompleted = true, CancellationToken cancellationToken = default);
    Task<bool> DeleteTaskAsync(int taskId, CancellationToken cancellationToken = default);

    Task<PomodoroSession> RecordSessionAsync(int userId, int? taskId, SessionType sessionType, int durationMinutes, DateTime startTime, DateTime? endTime = null, CancellationToken cancellationToken = default);
    Task<PomodoroSession> LinkSessionAsync(int userId, int? taskId, SessionType sessionType, int durationMinutes, DateTime startTime, DateTime? endTime = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<PomodoroSession>> GetSessionsByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PomodoroSession>> GetSessionsByTaskIdAsync(int taskId, CancellationToken cancellationToken = default);
}
