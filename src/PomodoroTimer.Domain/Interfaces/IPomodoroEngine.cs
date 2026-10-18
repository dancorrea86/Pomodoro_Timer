using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Enums;

namespace PomodoroTimer.Domain.Interfaces;

public interface IPomodoroEngine
{
    SessionType CurrentMode { get; }
    int RemainingSeconds { get; }
    TimerState State { get; }
    int FocusCycleCount { get; }

    void Start();
    void Pause();
    void Reset();
    void Skip();
    void Tick(int secondsElapsed);
    void ApplySettings(UserSettings settings);
}
