using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Enums;
using PomodoroTimer.Domain.Interfaces;

namespace PomodoroTimer.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;

    public TaskService(
        ITaskRepository taskRepository,
        IPomodoroSessionRepository sessionRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
    }

    public async Task<TaskItem> CreateTaskAsync(
        int userId,
        string title,
        string? description = null,
        int estimatedPomodoros = 1,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("O identificador do usuário é inválido.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("O título da tarefa é obrigatório.", nameof(title));
        }

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > 200)
        {
            throw new ArgumentException("O título da tarefa não pode exceder 200 caracteres.", nameof(title));
        }

        if (estimatedPomodoros < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedPomodoros), "A estimativa de pomodoros deve ser de pelo menos 1.");
        }

        var task = new TaskItem
        {
            UserId = userId,
            Title = trimmedTitle,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            EstimatedPomodoros = estimatedPomodoros,
            CompletedPomodoros = 0,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        return await _taskRepository.AddAsync(task, cancellationToken);
    }

    public async Task<TaskItem?> GetTaskByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _taskRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByUserIdAsync(
        int userId,
        bool? isCompleted = null,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("O identificador do usuário é inválido.", nameof(userId));
        }

        return await _taskRepository.GetByUserIdAsync(userId, isCompleted, cancellationToken);
    }

    public async Task<TaskItem> UpdateTaskAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        if (string.IsNullOrWhiteSpace(task.Title))
        {
            throw new ArgumentException("O título da tarefa é obrigatório.", nameof(task));
        }

        if (task.EstimatedPomodoros < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(task.EstimatedPomodoros), "A estimativa de pomodoros deve ser de pelo menos 1.");
        }

        await _taskRepository.UpdateAsync(task, cancellationToken);
        return task;
    }

    public async Task<TaskItem> UpdateTaskDetailsAsync(
        int taskId,
        string title,
        string? description,
        int estimatedPomodoros,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("O título da tarefa é obrigatório.", nameof(title));
        }

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > 200)
        {
            throw new ArgumentException("O título da tarefa não pode exceder 200 caracteres.", nameof(title));
        }

        if (estimatedPomodoros < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedPomodoros), "A estimativa de pomodoros deve ser de pelo menos 1.");
        }

        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
        {
            throw new KeyNotFoundException($"Tarefa com ID {taskId} não encontrada.");
        }

        task.Title = trimmedTitle;
        task.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        task.EstimatedPomodoros = estimatedPomodoros;

        await _taskRepository.UpdateAsync(task, cancellationToken);
        return task;
    }

    public async Task<TaskItem> CompleteTaskAsync(int taskId, bool isCompleted = true, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
        {
            throw new KeyNotFoundException($"Tarefa com ID {taskId} não encontrada.");
        }

        task.IsCompleted = isCompleted;
        await _taskRepository.UpdateAsync(task, cancellationToken);
        return task;
    }

    public async Task<bool> DeleteTaskAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
        {
            return false;
        }

        await _taskRepository.DeleteAsync(task, cancellationToken);
        return true;
    }

    public async Task<PomodoroSession> RecordSessionAsync(
        int userId,
        int? taskId,
        SessionType sessionType,
        int durationMinutes,
        DateTime startTime,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("O identificador do usuário é inválido.", nameof(userId));
        }

        if (durationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "A duração da sessão deve ser maior que zero.");
        }

        if (taskId.HasValue)
        {
            var task = await _taskRepository.GetByIdAsync(taskId.Value, cancellationToken);
            if (task is null)
            {
                throw new KeyNotFoundException($"Tarefa com ID {taskId.Value} não encontrada.");
            }

            if (task.UserId != userId)
            {
                throw new InvalidOperationException("A tarefa não pertence ao usuário especificado.");
            }

            if (sessionType == SessionType.Focus)
            {
                task.CompletedPomodoros++;
                await _taskRepository.UpdateAsync(task, cancellationToken);
            }
        }

        var session = new PomodoroSession
        {
            UserId = userId,
            TaskId = taskId,
            StartTime = startTime,
            EndTime = endTime ?? startTime.AddMinutes(durationMinutes),
            DurationMinutes = durationMinutes,
            Type = sessionType,
            IsCompleted = true
        };

        return await _sessionRepository.AddAsync(session, cancellationToken);
    }

    public Task<PomodoroSession> LinkSessionAsync(
        int userId,
        int? taskId,
        SessionType sessionType,
        int durationMinutes,
        DateTime startTime,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        return RecordSessionAsync(userId, taskId, sessionType, durationMinutes, startTime, endTime, cancellationToken);
    }

    public async Task<IEnumerable<PomodoroSession>> GetSessionsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("O identificador do usuário é inválido.", nameof(userId));
        }

        return await _sessionRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<PomodoroSession>> GetSessionsByTaskIdAsync(int taskId, CancellationToken cancellationToken = default)
    {
        if (taskId <= 0)
        {
            throw new ArgumentException("O identificador da tarefa é inválido.", nameof(taskId));
        }

        return await _sessionRepository.GetByTaskIdAsync(taskId, cancellationToken);
    }
}
