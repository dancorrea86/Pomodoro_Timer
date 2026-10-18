namespace PomodoroTimer.Domain.Entities;

public class UserSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int FocusDurationMinutes { get; set; } = 25;
    public int ShortBreakMinutes { get; set; } = 5;
    public int LongBreakMinutes { get; set; } = 15;
    public int CyclesBeforeLongBreak { get; set; } = 4;
    public bool SoundEnabled { get; set; } = true;

    // Navigation property
    public User User { get; set; } = null!;
}
