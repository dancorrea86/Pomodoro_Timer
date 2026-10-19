using Microsoft.EntityFrameworkCore;
using PomodoroTimer.Data.Context;
using PomodoroTimer.Data.Repositories;
using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Enums;
using PomodoroTimer.Services;
using Xunit;

namespace PomodoroTimer.Tests;

public class TaskIntegrationTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly DbContextOptions<PomodoroDbContext> _options;

    public TaskIntegrationTests()
    {
        var id = Guid.NewGuid().ToString("N");
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"pomodoro_task_test_{id}.db");

        _options = new DbContextOptionsBuilder<PomodoroDbContext>()
            .UseSqlite($"Data Source={_tempDbPath}")
            .Options;

        using var context = new PomodoroDbContext(_options);
        context.Database.Migrate();

        // Seed a user
        var user = new User
        {
            Username = "testuser",
            PasswordHash = "hash",
            Salt = "salt",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();
    }

    public void Dispose()
    {
        if (File.Exists(_tempDbPath))
        {
            try { File.Delete(_tempDbPath); } catch { }
        }
    }

    [Fact]
    public async Task TaskService_WithRealDatabase_EndToEndCrudAndSessionLinking()
    {
        int createdTaskId;
        int userId = 1;

        // 1. Create task in first DbContext scope
        using (var context = new PomodoroDbContext(_options))
        {
            var taskRepo = new TaskRepository(context);
            var sessionRepo = new PomodoroSessionRepository(context);
            var service = new TaskService(taskRepo, sessionRepo);

            var task = await service.CreateTaskAsync(userId, "Write code", "Backend API implementation", 3);
            Assert.True(task.Id > 0);
            createdTaskId = task.Id;
        }

        // 2. Read and update task in second DbContext scope
        using (var context = new PomodoroDbContext(_options))
        {
            var taskRepo = new TaskRepository(context);
            var sessionRepo = new PomodoroSessionRepository(context);
            var service = new TaskService(taskRepo, sessionRepo);

            var retrieved = await service.GetTaskByIdAsync(createdTaskId);
            Assert.NotNull(retrieved);
            Assert.Equal("Write code", retrieved.Title);
            Assert.Equal(0, retrieved.CompletedPomodoros);

            await service.UpdateTaskDetailsAsync(createdTaskId, "Write robust code", "With tests", 4);
        }

        // 3. Record sessions linked to task in third DbContext scope
        using (var context = new PomodoroDbContext(_options))
        {
            var taskRepo = new TaskRepository(context);
            var sessionRepo = new PomodoroSessionRepository(context);
            var service = new TaskService(taskRepo, sessionRepo);

            // Record focus session
            var session1 = await service.RecordSessionAsync(
                userId,
                createdTaskId,
                SessionType.Focus,
                25,
                DateTime.UtcNow.AddMinutes(-30),
                DateTime.UtcNow.AddMinutes(-5));

            Assert.True(session1.Id > 0);

            // Record short break session
            var session2 = await service.RecordSessionAsync(
                userId,
                createdTaskId,
                SessionType.ShortBreak,
                5,
                DateTime.UtcNow.AddMinutes(-5),
                DateTime.UtcNow);

            Assert.True(session2.Id > 0);
        }

        // 4. Verify task pomodoros count updated and sessions queryable
        using (var context = new PomodoroDbContext(_options))
        {
            var taskRepo = new TaskRepository(context);
            var sessionRepo = new PomodoroSessionRepository(context);
            var service = new TaskService(taskRepo, sessionRepo);

            var task = await service.GetTaskByIdAsync(createdTaskId);
            Assert.NotNull(task);
            Assert.Equal(1, task.CompletedPomodoros);
            Assert.Equal("Write robust code", task.Title);
            Assert.Equal(4, task.EstimatedPomodoros);

            var taskSessions = (await service.GetSessionsByTaskIdAsync(createdTaskId)).ToList();
            Assert.Equal(2, taskSessions.Count);

            var userSessions = (await service.GetSessionsByUserIdAsync(userId)).ToList();
            Assert.Equal(2, userSessions.Count);

            // Complete task
            var completedTask = await service.CompleteTaskAsync(createdTaskId);
            Assert.True(completedTask.IsCompleted);

            // Filter uncompleted tasks
            var pendingTasks = (await service.GetTasksByUserIdAsync(userId, isCompleted: false)).ToList();
            Assert.Empty(pendingTasks);

            var finishedTasks = (await service.GetTasksByUserIdAsync(userId, isCompleted: true)).ToList();
            Assert.Single(finishedTasks);

            // Delete task and verify session SetNull behavior
            var deleted = await service.DeleteTaskAsync(createdTaskId);
            Assert.True(deleted);

            var deletedLookup = await service.GetTaskByIdAsync(createdTaskId);
            Assert.Null(deletedLookup);

            // Sessions should remain in history with TaskId set to null
            var remainingUserSessions = (await service.GetSessionsByUserIdAsync(userId)).ToList();
            Assert.Equal(2, remainingUserSessions.Count);
            Assert.All(remainingUserSessions, s => Assert.Null(s.TaskId));
        }
    }
}
