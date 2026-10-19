using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Enums;
using PomodoroTimer.Domain.Interfaces;
using PomodoroTimer.Services;
using Xunit;

namespace PomodoroTimer.Tests;

public class TaskServiceTests
{
    private readonly FakeTaskRepository _taskRepository = new();
    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly TaskService _taskService;

    public TaskServiceTests()
    {
        _taskService = new TaskService(_taskRepository, _sessionRepository);
    }

    #region Task CRUD Operations

    [Fact]
    public async Task CreateTaskAsync_ShouldCreateTask_WithDefaultValues()
    {
        var task = await _taskService.CreateTaskAsync(userId: 1, title: "Refactor architecture");

        Assert.NotNull(task);
        Assert.Equal(1, task.Id);
        Assert.Equal(1, task.UserId);
        Assert.Equal("Refactor architecture", task.Title);
        Assert.Null(task.Description);
        Assert.Equal(1, task.EstimatedPomodoros);
        Assert.Equal(0, task.CompletedPomodoros);
        Assert.False(task.IsCompleted);
        Assert.True(task.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldTrimTitleAndDescription_AndSetEstimatedPomodoros()
    {
        var task = await _taskService.CreateTaskAsync(
            userId: 2,
            title: "  Document API  ",
            description: "  Write swagger docs  ",
            estimatedPomodoros: 5);

        Assert.Equal("Document API", task.Title);
        Assert.Equal("Write swagger docs", task.Description);
        Assert.Equal(5, task.EstimatedPomodoros);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateTaskAsync_ShouldThrow_WhenTitleIsEmpty(string? title)
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _taskService.CreateTaskAsync(userId: 1, title: title!));

        Assert.Contains("obrigatório", ex.Message);
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldThrow_WhenTitleExceeds200Characters()
    {
        var longTitle = new string('A', 201);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _taskService.CreateTaskAsync(userId: 1, title: longTitle));

        Assert.Contains("200 caracteres", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateTaskAsync_ShouldThrow_WhenUserIdIsInvalid(int invalidUserId)
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _taskService.CreateTaskAsync(userId: invalidUserId, title: "Valid Title"));

        Assert.Contains("inválido", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task CreateTaskAsync_ShouldThrow_WhenEstimatedPomodorosIsLessThanOne(int invalidEstimate)
    {
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _taskService.CreateTaskAsync(userId: 1, title: "Valid Title", estimatedPomodoros: invalidEstimate));

        Assert.Contains("pelo menos 1", ex.Message);
    }

    [Fact]
    public async Task GetTaskByIdAsync_ShouldReturnTask_WhenTaskExists()
    {
        var created = await _taskService.CreateTaskAsync(userId: 1, title: "Find me");

        var found = await _taskService.GetTaskByIdAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found.Id);
        Assert.Equal("Find me", found.Title);
    }

    [Fact]
    public async Task GetTaskByIdAsync_ShouldReturnNull_WhenTaskDoesNotExist()
    {
        var found = await _taskService.GetTaskByIdAsync(999);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetTasksByUserIdAsync_ShouldFilterByCompletionStatus()
    {
        var task1 = await _taskService.CreateTaskAsync(userId: 1, title: "Pending Task 1");
        var task2 = await _taskService.CreateTaskAsync(userId: 1, title: "Completed Task 1");
        var task3 = await _taskService.CreateTaskAsync(userId: 2, title: "Other User Task");

        await _taskService.CompleteTaskAsync(task2.Id, isCompleted: true);

        var allUser1Tasks = (await _taskService.GetTasksByUserIdAsync(userId: 1)).ToList();
        var pendingUser1Tasks = (await _taskService.GetTasksByUserIdAsync(userId: 1, isCompleted: false)).ToList();
        var completedUser1Tasks = (await _taskService.GetTasksByUserIdAsync(userId: 1, isCompleted: true)).ToList();

        Assert.Equal(2, allUser1Tasks.Count);
        Assert.Single(pendingUser1Tasks);
        Assert.Equal(task1.Id, pendingUser1Tasks[0].Id);
        Assert.Single(completedUser1Tasks);
        Assert.Equal(task2.Id, completedUser1Tasks[0].Id);
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldUpdateTaskProperties()
    {
        var task = await _taskService.CreateTaskAsync(userId: 1, title: "Initial Title", estimatedPomodoros: 2);

        task.Title = "Updated Title";
        task.Description = "New Description";
        task.EstimatedPomodoros = 4;
        await _taskService.UpdateTaskAsync(task);

        var retrieved = await _taskService.GetTaskByIdAsync(task.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Title", retrieved.Title);
        Assert.Equal("New Description", retrieved.Description);
        Assert.Equal(4, retrieved.EstimatedPomodoros);
    }

    [Fact]
    public async Task UpdateTaskDetailsAsync_ShouldModifyFieldsAndSave()
    {
        var task = await _taskService.CreateTaskAsync(userId: 1, title: "Original Title", estimatedPomodoros: 1);

        var updated = await _taskService.UpdateTaskDetailsAsync(task.Id, "Modified Title", "New Notes", 3);

        Assert.Equal("Modified Title", updated.Title);
        Assert.Equal("New Notes", updated.Description);
        Assert.Equal(3, updated.EstimatedPomodoros);
    }

    [Fact]
    public async Task UpdateTaskDetailsAsync_ShouldThrow_WhenTaskNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _taskService.UpdateTaskDetailsAsync(999, "Title", null, 1));
    }

    [Fact]
    public async Task CompleteTaskAsync_ShouldToggleTaskStatus()
    {
        var task = await _taskService.CreateTaskAsync(userId: 1, title: "Complete Me");

        Assert.False(task.IsCompleted);

        var completed = await _taskService.CompleteTaskAsync(task.Id, isCompleted: true);
        Assert.True(completed.IsCompleted);

        var uncompleted = await _taskService.CompleteTaskAsync(task.Id, isCompleted: false);
        Assert.False(uncompleted.IsCompleted);
    }

    [Fact]
    public async Task DeleteTaskAsync_ShouldRemoveTask_AndReturnTrue()
    {
        var task = await _taskService.CreateTaskAsync(userId: 1, title: "Delete Me");

        var deleted = await _taskService.DeleteTaskAsync(task.Id);
        Assert.True(deleted);

        var found = await _taskService.GetTaskByIdAsync(task.Id);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteTaskAsync_ShouldReturnFalse_WhenTaskDoesNotExist()
    {
        var deleted = await _taskService.DeleteTaskAsync(999);
        Assert.False(deleted);
    }

    #endregion

    #region Session Linking & Logging

    [Fact]
    public async Task RecordSessionAsync_WithoutTask_ShouldSaveSessionSuccessfully()
    {
        var startTime = DateTime.UtcNow.AddMinutes(-25);
        var endTime = DateTime.UtcNow;

        var session = await _taskService.RecordSessionAsync(
            userId: 1,
            taskId: null,
            sessionType: SessionType.Focus,
            durationMinutes: 25,
            startTime: startTime,
            endTime: endTime);

        Assert.NotNull(session);
        Assert.Equal(1, session.Id);
        Assert.Equal(1, session.UserId);
        Assert.Null(session.TaskId);
        Assert.Equal(SessionType.Focus, session.Type);
        Assert.Equal(25, session.DurationMinutes);
        Assert.True(session.IsCompleted);
    }

    [Fact]
    public async Task RecordSessionAsync_FocusWithTask_ShouldIncrementCompletedPomodoros_AndLinkSession()
    {
        var task = await _taskService.CreateTaskAsync(userId: 1, title: "Focus Task", estimatedPomodoros: 4);
        Assert.Equal(0, task.CompletedPomodoros);

        var startTime = DateTime.UtcNow.AddMinutes(-25);

        var session = await _taskService.RecordSessionAsync(
            userId: 1,
            taskId: task.Id,
            sessionType: SessionType.Focus,
            durationMinutes: 25,
            startTime: startTime);

        Assert.NotNull(session);
        Assert.Equal(task.Id, session.TaskId);
        Assert.Equal(SessionType.Focus, session.Type);

        var updatedTask = await _taskService.GetTaskByIdAsync(task.Id);
        Assert.NotNull(updatedTask);
        Assert.Equal(1, updatedTask.CompletedPomodoros);
    }

    [Fact]
    public async Task RecordSessionAsync_BreakSessionWithTask_ShouldNotIncrementCompletedPomodoros()
    {
        var task = await _taskService.CreateTaskAsync(userId: 1, title: "Break Task", estimatedPomodoros: 4);

        var session = await _taskService.RecordSessionAsync(
            userId: 1,
            taskId: task.Id,
            sessionType: SessionType.ShortBreak,
            durationMinutes: 5,
            startTime: DateTime.UtcNow.AddMinutes(-5));

        Assert.NotNull(session);
        Assert.Equal(task.Id, session.TaskId);
        Assert.Equal(SessionType.ShortBreak, session.Type);

        var updatedTask = await _taskService.GetTaskByIdAsync(task.Id);
        Assert.NotNull(updatedTask);
        Assert.Equal(0, updatedTask.CompletedPomodoros);
    }

    [Fact]
    public async Task RecordSessionAsync_ShouldThrow_WhenTaskDoesNotExist()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _taskService.RecordSessionAsync(
                userId: 1,
                taskId: 999,
                sessionType: SessionType.Focus,
                durationMinutes: 25,
                startTime: DateTime.UtcNow));
    }

    [Fact]
    public async Task RecordSessionAsync_ShouldThrow_WhenTaskBelongsToDifferentUser()
    {
        var otherUserTask = await _taskService.CreateTaskAsync(userId: 2, title: "Other User Task");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _taskService.RecordSessionAsync(
                userId: 1,
                taskId: otherUserTask.Id,
                sessionType: SessionType.Focus,
                durationMinutes: 25,
                startTime: DateTime.UtcNow));
    }

    [Fact]
    public async Task LinkSessionAsync_ShouldBehaveIdenticalToRecordSession()
    {
        var task = await _taskService.CreateTaskAsync(userId: 1, title: "Link Task");

        var session = await _taskService.LinkSessionAsync(
            userId: 1,
            taskId: task.Id,
            sessionType: SessionType.Focus,
            durationMinutes: 25,
            startTime: DateTime.UtcNow.AddMinutes(-25));

        Assert.NotNull(session);
        Assert.Equal(task.Id, session.TaskId);

        var updatedTask = await _taskService.GetTaskByIdAsync(task.Id);
        Assert.Equal(1, updatedTask!.CompletedPomodoros);
    }

    [Fact]
    public async Task GetSessionsByUserIdAndTaskId_ShouldReturnCorrectSessions()
    {
        var task1 = await _taskService.CreateTaskAsync(userId: 1, title: "Task 1");
        var task2 = await _taskService.CreateTaskAsync(userId: 1, title: "Task 2");

        await _taskService.RecordSessionAsync(1, task1.Id, SessionType.Focus, 25, DateTime.UtcNow.AddMinutes(-50));
        await _taskService.RecordSessionAsync(1, task1.Id, SessionType.ShortBreak, 5, DateTime.UtcNow.AddMinutes(-25));
        await _taskService.RecordSessionAsync(1, task2.Id, SessionType.Focus, 25, DateTime.UtcNow.AddMinutes(-20));
        await _taskService.RecordSessionAsync(1, null, SessionType.LongBreak, 15, DateTime.UtcNow);

        var userSessions = (await _taskService.GetSessionsByUserIdAsync(1)).ToList();
        var task1Sessions = (await _taskService.GetSessionsByTaskIdAsync(task1.Id)).ToList();
        var task2Sessions = (await _taskService.GetSessionsByTaskIdAsync(task2.Id)).ToList();

        Assert.Equal(4, userSessions.Count);
        Assert.Equal(2, task1Sessions.Count);
        Assert.Single(task2Sessions);
    }

    #endregion

    #region Fakes

    private sealed class FakeTaskRepository : ITaskRepository
    {
        private readonly List<TaskItem> _tasks = new();

        public Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id));

        public Task<IEnumerable<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<TaskItem>>(_tasks);

        public Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId, bool? isCompleted = null, CancellationToken cancellationToken = default)
        {
            var query = _tasks.Where(t => t.UserId == userId);
            if (isCompleted.HasValue)
            {
                query = query.Where(t => t.IsCompleted == isCompleted.Value);
            }
            return Task.FromResult<IEnumerable<TaskItem>>(query.OrderByDescending(t => t.CreatedAt).ToList());
        }

        public Task<TaskItem> AddAsync(TaskItem entity, CancellationToken cancellationToken = default)
        {
            entity.Id = _tasks.Count + 1;
            _tasks.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(TaskItem entity, CancellationToken cancellationToken = default)
        {
            var index = _tasks.FindIndex(t => t.Id == entity.Id);
            if (index >= 0)
            {
                _tasks[index] = entity;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TaskItem entity, CancellationToken cancellationToken = default)
        {
            _tasks.RemoveAll(t => t.Id == entity.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePomodoroSessionRepository : IPomodoroSessionRepository
    {
        private readonly List<PomodoroSession> _sessions = new();

        public Task<PomodoroSession?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_sessions.FirstOrDefault(s => s.Id == id));

        public Task<IEnumerable<PomodoroSession>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<PomodoroSession>>(_sessions);

        public Task<IEnumerable<PomodoroSession>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<PomodoroSession>>(_sessions.Where(s => s.UserId == userId).OrderByDescending(s => s.StartTime).ToList());

        public Task<IEnumerable<PomodoroSession>> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<PomodoroSession>>(_sessions.Where(s => s.TaskId == taskId).OrderByDescending(s => s.StartTime).ToList());

        public Task<PomodoroSession> AddAsync(PomodoroSession entity, CancellationToken cancellationToken = default)
        {
            entity.Id = _sessions.Count + 1;
            _sessions.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(PomodoroSession entity, CancellationToken cancellationToken = default)
        {
            var index = _sessions.FindIndex(s => s.Id == entity.Id);
            if (index >= 0)
            {
                _sessions[index] = entity;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(PomodoroSession entity, CancellationToken cancellationToken = default)
        {
            _sessions.RemoveAll(s => s.Id == entity.Id);
            return Task.CompletedTask;
        }
    }

    #endregion
}
