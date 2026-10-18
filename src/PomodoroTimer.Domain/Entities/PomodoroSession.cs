using PomodoroTimer.Domain.Enums;

namespace PomodoroTimer.Domain.Entities;

public class PomodoroSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? TaskId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public SessionType Type { get; set; }
    public bool IsCompleted { get; set; } = true;

    // Navigation properties
    public User User { get; set; } = null!;
    public TaskItem? Task { get; set; }
}
