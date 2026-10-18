using System.Text.Json;
using PomodoroTimer.Domain.Interfaces;

namespace PomodoroTimer.Services.Storage;

public class FileSessionStorage : ISessionStorage
{
    private readonly string _sessionFilePath;

    public FileSessionStorage(string? sessionFilePath = null)
    {
        if (string.IsNullOrWhiteSpace(sessionFilePath))
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbDirectory = Path.Combine(appDataFolder, "PomodoroTimer");
            _sessionFilePath = Path.Combine(dbDirectory, "session.json");
        }
        else
        {
            _sessionFilePath = sessionFilePath;
        }
    }

    public async Task SaveSessionAsync(int userId, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_sessionFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var sessionData = new SessionData
        {
            UserId = userId,
            SavedAtUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(sessionData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_sessionFilePath, json, cancellationToken);
    }

    public async Task<int?> GetSavedUserIdAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_sessionFilePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_sessionFilePath, cancellationToken);
            var sessionData = JsonSerializer.Deserialize<SessionData>(json);
            return sessionData?.UserId;
        }
        catch
        {
            return null;
        }
    }

    public Task ClearSessionAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_sessionFilePath))
        {
            try
            {
                File.Delete(_sessionFilePath);
            }
            catch
            {
                // Best-effort session removal
            }
        }

        return Task.CompletedTask;
    }

    private sealed class SessionData
    {
        public int UserId { get; set; }
        public DateTime SavedAtUtc { get; set; }
    }
}
