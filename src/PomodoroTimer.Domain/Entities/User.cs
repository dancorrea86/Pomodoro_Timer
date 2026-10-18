namespace PomodoroTimer.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? SettingsId { get; set; }

    // Navigation properties
    public UserSettings? Settings { get; set; }
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<PomodoroSession> Sessions { get; set; } = new List<PomodoroSession>();
}
