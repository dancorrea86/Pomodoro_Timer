using Microsoft.EntityFrameworkCore;
using PomodoroTimer.Data.Context;
using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Enums;
using Xunit;

namespace PomodoroTimer.Tests;

public class DatabaseMigrationTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly DbContextOptions<PomodoroDbContext> _options;

    public DatabaseMigrationTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"pomodoro_test_{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<PomodoroDbContext>()
            .UseSqlite($"Data Source={_tempDbPath}")
            .Options;
    }

    public void Dispose()
    {
        if (File.Exists(_tempDbPath))
        {
            try
            {
                File.Delete(_tempDbPath);
            }
            catch
            {
                // Best effort cleanup in tests
            }
        }
    }

    [Fact]
    public void Can_Apply_Migrations_And_Generate_SQLite_Database()
    {
        // Act
        using (var context = new PomodoroDbContext(_options))
        {
            context.Database.Migrate();

            // Assert
            Assert.True(context.Database.CanConnect());
            Assert.True(File.Exists(_tempDbPath));

            var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
            Assert.Contains(appliedMigrations, m => m.Contains("InitialCreate"));

            var pendingMigrations = context.Database.GetPendingMigrations().ToList();
            Assert.Empty(pendingMigrations);
        }
    }

    [Fact]
    public void Can_Persist_And_Retrieve_Entities_With_Relationships()
    {
        // Arrange & Migrate
        using (var context = new PomodoroDbContext(_options))
        {
            context.Database.Migrate();

            var user = new User
            {
                Username = "testuser",
                PasswordHash = "hashedpassword",
                Salt = "randomsalt",
                CreatedAt = DateTime.UtcNow,
                Settings = new UserSettings
                {
                    FocusDurationMinutes = 25,
                    ShortBreakMinutes = 5,
                    LongBreakMinutes = 15,
                    CyclesBeforeLongBreak = 4,
                    SoundEnabled = true
                }
            };

            var task = new TaskItem
            {
                User = user,
                Title = "Implement T-105",
                Description = "Database migration and testing",
                IsCompleted = false,
                EstimatedPomodoros = 2,
                CompletedPomodoros = 1,
                CreatedAt = DateTime.UtcNow
            };

            var session = new PomodoroSession
            {
                User = user,
                Task = task,
                StartTime = DateTime.UtcNow.AddMinutes(-25),
                EndTime = DateTime.UtcNow,
                DurationMinutes = 25,
                Type = SessionType.Focus,
                IsCompleted = true
            };

            context.Users.Add(user);
            context.TaskItems.Add(task);
            context.PomodoroSessions.Add(session);
            context.SaveChanges();
        }

        // Act & Assert in a new context instance
        using (var context = new PomodoroDbContext(_options))
        {
            var savedUser = context.Users
                .AsNoTracking()
                .Include(u => u.Settings)
                .Include(u => u.Tasks)
                .Include(u => u.Sessions)
                .FirstOrDefault(u => u.Username == "testuser");

            Assert.NotNull(savedUser);
            Assert.Equal("testuser", savedUser.Username);

            Assert.NotNull(savedUser.Settings);
            Assert.Equal(25, savedUser.Settings.FocusDurationMinutes);
            Assert.True(savedUser.Settings.SoundEnabled);

            Assert.Single(savedUser.Tasks);
            var savedTask = savedUser.Tasks.First();
            Assert.Equal("Implement T-105", savedTask.Title);
            Assert.Equal(1, savedTask.CompletedPomodoros);

            Assert.Single(savedUser.Sessions);
            var savedSession = savedUser.Sessions.First();
            Assert.Equal(SessionType.Focus, savedSession.Type);
            Assert.Equal(savedTask.Id, savedSession.TaskId);
            Assert.True(savedSession.IsCompleted);
        }
    }

    [Fact]
    public void TaskItem_Deletion_Sets_PomodoroSession_TaskId_To_Null()
    {
        int sessionId;
        int taskId;

        using (var context = new PomodoroDbContext(_options))
        {
            context.Database.Migrate();

            var user = new User
            {
                Username = "user_task_null_test",
                PasswordHash = "hash",
                Salt = "salt"
            };

            var task = new TaskItem
            {
                User = user,
                Title = "Task to be deleted"
            };

            var session = new PomodoroSession
            {
                User = user,
                Task = task,
                StartTime = DateTime.UtcNow,
                DurationMinutes = 25,
                Type = SessionType.Focus
            };

            context.Users.Add(user);
            context.TaskItems.Add(task);
            context.PomodoroSessions.Add(session);
            context.SaveChanges();

            sessionId = session.Id;
            taskId = task.Id;
        }

        using (var context = new PomodoroDbContext(_options))
        {
            var taskToDelete = context.TaskItems.Find(taskId);
            Assert.NotNull(taskToDelete);

            context.TaskItems.Remove(taskToDelete);
            context.SaveChanges();
        }

        using (var context = new PomodoroDbContext(_options))
        {
            var session = context.PomodoroSessions.Find(sessionId);
            Assert.NotNull(session);
            Assert.Null(session.TaskId);
        }
    }
}
