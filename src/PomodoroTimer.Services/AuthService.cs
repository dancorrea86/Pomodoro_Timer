using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Interfaces;
using PomodoroTimer.Domain.Models;
using PomodoroTimer.Services.Security;
using PomodoroTimer.Services.Storage;

namespace PomodoroTimer.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionStorage _sessionStorage;

    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher? passwordHasher = null,
        ISessionStorage? sessionStorage = null)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? new Pbkdf2PasswordHasher();
        _sessionStorage = sessionStorage ?? new FileSessionStorage();
    }

    public async Task<AuthResult> RegisterAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var trimmedUsername = username?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedUsername))
        {
            return AuthResult.Fail("O nome de usuário é obrigatório.");
        }

        if (trimmedUsername.Length < 3)
        {
            return AuthResult.Fail("O nome de usuário deve ter no mínimo 3 caracteres.");
        }

        if (string.IsNullOrEmpty(password) || password.Length < 6)
        {
            return AuthResult.Fail("A senha deve ter no mínimo 6 caracteres.");
        }

        var exists = await _userRepository.UsernameExistsAsync(trimmedUsername, cancellationToken);
        if (exists)
        {
            return AuthResult.Fail("O nome de usuário já está em uso.");
        }

        var salt = _passwordHasher.GenerateSalt();
        var passwordHash = _passwordHasher.HashPassword(password, salt);

        var user = new User
        {
            Username = trimmedUsername,
            PasswordHash = passwordHash,
            Salt = salt,
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

        var createdUser = await _userRepository.AddAsync(user, cancellationToken);
        CurrentUser = createdUser;
        await _sessionStorage.SaveSessionAsync(createdUser.Id, cancellationToken);

        return AuthResult.Ok(createdUser);
    }

    public async Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var trimmedUsername = username?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedUsername) || string.IsNullOrEmpty(password))
        {
            return AuthResult.Fail("Nome de usuário e senha são obrigatórios.");
        }

        var user = await _userRepository.GetByUsernameAsync(trimmedUsername, cancellationToken);
        if (user == null)
        {
            return AuthResult.Fail("Nome de usuário ou senha inválidos.");
        }

        var isValidPassword = _passwordHasher.VerifyPassword(password, user.PasswordHash, user.Salt);
        if (!isValidPassword)
        {
            return AuthResult.Fail("Nome de usuário ou senha inválidos.");
        }

        CurrentUser = user;
        await _sessionStorage.SaveSessionAsync(user.Id, cancellationToken);

        return AuthResult.Ok(user);
    }

    public async Task<User?> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        var savedUserId = await _sessionStorage.GetSavedUserIdAsync(cancellationToken);
        if (!savedUserId.HasValue)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(savedUserId.Value, cancellationToken);
        if (user == null)
        {
            await _sessionStorage.ClearSessionAsync(cancellationToken);
            return null;
        }

        CurrentUser = user;
        return user;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        CurrentUser = null;
        await _sessionStorage.ClearSessionAsync(cancellationToken);
    }
}
