using Microsoft.EntityFrameworkCore;
using PomodoroTimer.Data.Context;
using PomodoroTimer.Data.Repositories;
using PomodoroTimer.Services;
using PomodoroTimer.Services.Security;
using PomodoroTimer.Services.Storage;
using Xunit;

namespace PomodoroTimer.Tests;

public class UserRepositoryAndAuthIntegrationTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly string _tempSessionPath;
    private readonly DbContextOptions<PomodoroDbContext> _options;

    public UserRepositoryAndAuthIntegrationTests()
    {
        var id = Guid.NewGuid().ToString("N");
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"pomodoro_auth_test_{id}.db");
        _tempSessionPath = Path.Combine(Path.GetTempPath(), $"pomodoro_auth_session_{id}.json");

        _options = new DbContextOptionsBuilder<PomodoroDbContext>()
            .UseSqlite($"Data Source={_tempDbPath}")
            .Options;

        // Apply migrations
        using var context = new PomodoroDbContext(_options);
        context.Database.Migrate();
    }

    public void Dispose()
    {
        if (File.Exists(_tempDbPath))
        {
            try { File.Delete(_tempDbPath); } catch { }
        }

        if (File.Exists(_tempSessionPath))
        {
            try { File.Delete(_tempSessionPath); } catch { }
        }
    }

    [Fact]
    public async Task EndToEnd_Register_PersistInDatabase_Login_And_RestoreSession()
    {
        // 1. Register a user in the first scope
        using (var context = new PomodoroDbContext(_options))
        {
            var userRepo = new UserRepository(context);
            var sessionStorage = new FileSessionStorage(_tempSessionPath);
            var hasher = new Pbkdf2PasswordHasher();
            var authService = new AuthService(userRepo, hasher, sessionStorage);

            var registerResult = await authService.RegisterAsync("integrationuser", "Password#2026");

            Assert.True(registerResult.Success);
            Assert.NotNull(registerResult.User);
            Assert.True(registerResult.User.Id > 0);
            Assert.Equal("integrationuser", registerResult.User.Username);
            Assert.True(authService.IsAuthenticated);
        }

        // 2. Open new context & service instance (simulating app restart)
        using (var context = new PomodoroDbContext(_options))
        {
            var userRepo = new UserRepository(context);
            var sessionStorage = new FileSessionStorage(_tempSessionPath);
            var hasher = new Pbkdf2PasswordHasher();
            var authService = new AuthService(userRepo, hasher, sessionStorage);

            Assert.False(authService.IsAuthenticated);

            // Restore session
            var restoredUser = await authService.TryRestoreSessionAsync();

            Assert.NotNull(restoredUser);
            Assert.Equal("integrationuser", restoredUser.Username);
            Assert.NotNull(restoredUser.Settings);
            Assert.Equal(25, restoredUser.Settings.FocusDurationMinutes);
            Assert.True(authService.IsAuthenticated);

            // Logout
            await authService.LogoutAsync();
            Assert.False(authService.IsAuthenticated);
        }

        // 3. Open another context instance to verify logout persisted and clean login works
        using (var context = new PomodoroDbContext(_options))
        {
            var userRepo = new UserRepository(context);
            var sessionStorage = new FileSessionStorage(_tempSessionPath);
            var hasher = new Pbkdf2PasswordHasher();
            var authService = new AuthService(userRepo, hasher, sessionStorage);

            var sessionUser = await authService.TryRestoreSessionAsync();
            Assert.Null(sessionUser);
            Assert.False(authService.IsAuthenticated);

            // Re-login with valid credentials
            var loginResult = await authService.LoginAsync("integrationuser", "Password#2026");
            Assert.True(loginResult.Success);
            Assert.NotNull(loginResult.User);
            Assert.True(authService.IsAuthenticated);
        }
    }
}
