using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Interfaces;
using PomodoroTimer.Services;
using PomodoroTimer.Services.Security;
using Xunit;

namespace PomodoroTimer.Tests;

public class AuthServiceTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeSessionStorage _sessionStorage = new();
    private readonly Pbkdf2PasswordHasher _passwordHasher = new();
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _authService = new AuthService(_userRepository, _passwordHasher, _sessionStorage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RegisterAsync_ShouldFail_WhenUsernameIsEmpty(string? username)
    {
        var result = await _authService.RegisterAsync(username!, "validpassword123");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.User);
        Assert.Null(_authService.CurrentUser);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    public async Task RegisterAsync_ShouldFail_WhenUsernameIsTooShort(string username)
    {
        var result = await _authService.RegisterAsync(username, "validpassword123");

        Assert.False(result.Success);
        Assert.Contains("3 caracteres", result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData(null)]
    public async Task RegisterAsync_ShouldFail_WhenPasswordIsTooShort(string? password)
    {
        var result = await _authService.RegisterAsync("validuser", password!);

        Assert.False(result.Success);
        Assert.Contains("6 caracteres", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WithHashedPassword_AndDefaultSettings()
    {
        var result = await _authService.RegisterAsync("alice", "securepassword123");

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal("alice", result.User.Username);
        Assert.NotEqual("securepassword123", result.User.PasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(result.User.Salt));

        // Verifies default user settings
        Assert.NotNull(result.User.Settings);
        Assert.Equal(25, result.User.Settings.FocusDurationMinutes);
        Assert.Equal(5, result.User.Settings.ShortBreakMinutes);
        Assert.Equal(15, result.User.Settings.LongBreakMinutes);
        Assert.Equal(4, result.User.Settings.CyclesBeforeLongBreak);
        Assert.True(result.User.Settings.SoundEnabled);

        // Current session set
        Assert.Equal(result.User, _authService.CurrentUser);
        Assert.True(_authService.IsAuthenticated);
        Assert.Equal(result.User.Id, _sessionStorage.StoredUserId);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFail_WhenUsernameAlreadyExists_CaseInsensitive()
    {
        await _authService.RegisterAsync("bob", "password123");

        var duplicateResult = await _authService.RegisterAsync("BOB", "differentpassword");

        Assert.False(duplicateResult.Success);
        Assert.Contains("já está em uso", duplicateResult.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ShouldSucceed_WithValidCredentials()
    {
        await _authService.RegisterAsync("charlie", "mypassword123");
        await _authService.LogoutAsync();

        Assert.Null(_authService.CurrentUser);
        Assert.False(_authService.IsAuthenticated);

        var loginResult = await _authService.LoginAsync("charlie", "mypassword123");

        Assert.True(loginResult.Success);
        Assert.NotNull(loginResult.User);
        Assert.Equal("charlie", loginResult.User.Username);
        Assert.True(_authService.IsAuthenticated);
        Assert.Equal(loginResult.User.Id, _sessionStorage.StoredUserId);
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WithIncorrectPassword()
    {
        await _authService.RegisterAsync("david", "correctpassword");
        await _authService.LogoutAsync();

        var loginResult = await _authService.LoginAsync("david", "wrongpassword");

        Assert.False(loginResult.Success);
        Assert.Contains("inválidos", loginResult.ErrorMessage);
        Assert.False(_authService.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenUserDoesNotExist()
    {
        var loginResult = await _authService.LoginAsync("nonexistent", "password123");

        Assert.False(loginResult.Success);
        Assert.False(_authService.IsAuthenticated);
    }

    [Fact]
    public async Task TryRestoreSessionAsync_ShouldRestoreSession_WhenSavedUserExists()
    {
        var registerResult = await _authService.RegisterAsync("eva", "password123");
        var userId = registerResult.User!.Id;

        // Simulate new app launch with same storage
        var newAuthService = new AuthService(_userRepository, _passwordHasher, _sessionStorage);
        Assert.False(newAuthService.IsAuthenticated);

        var restoredUser = await newAuthService.TryRestoreSessionAsync();

        Assert.NotNull(restoredUser);
        Assert.Equal(userId, restoredUser.Id);
        Assert.Equal("eva", restoredUser.Username);
        Assert.True(newAuthService.IsAuthenticated);
    }

    [Fact]
    public async Task TryRestoreSessionAsync_ShouldReturnNullAndClearStorage_WhenSavedUserDoesNotExist()
    {
        await _sessionStorage.SaveSessionAsync(999);

        var restoredUser = await _authService.TryRestoreSessionAsync();

        Assert.Null(restoredUser);
        Assert.Null(_sessionStorage.StoredUserId);
        Assert.False(_authService.IsAuthenticated);
    }

    [Fact]
    public async Task LogoutAsync_ShouldClearCurrentUser_AndSessionStorage()
    {
        await _authService.RegisterAsync("frank", "password123");
        Assert.True(_authService.IsAuthenticated);
        Assert.NotNull(_sessionStorage.StoredUserId);

        await _authService.LogoutAsync();

        Assert.Null(_authService.CurrentUser);
        Assert.False(_authService.IsAuthenticated);
        Assert.Null(_sessionStorage.StoredUserId);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<User>>(_users);

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));

        public Task<User> AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            entity.Id = _users.Count + 1;
            _users.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(User entity, CancellationToken cancellationToken = default)
        {
            var existing = _users.FindIndex(u => u.Id == entity.Id);
            if (existing >= 0) _users[existing] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(User entity, CancellationToken cancellationToken = default)
        {
            _users.RemoveAll(u => u.Id == entity.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSessionStorage : ISessionStorage
    {
        public int? StoredUserId { get; private set; }

        public Task SaveSessionAsync(int userId, CancellationToken cancellationToken = default)
        {
            StoredUserId = userId;
            return Task.CompletedTask;
        }

        public Task<int?> GetSavedUserIdAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StoredUserId);
        }

        public Task ClearSessionAsync(CancellationToken cancellationToken = default)
        {
            StoredUserId = null;
            return Task.CompletedTask;
        }
    }
}
