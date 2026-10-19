using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Enums;
using PomodoroTimer.Domain.Events;

namespace PomodoroTimer.Domain.Interfaces;

public interface IPomodoroEngine
{
    SessionType CurrentMode { get; }
    int RemainingSeconds { get; }
    TimerState State { get; }
    int FocusCycleCount { get; }

    event EventHandler<PomodoroCompletedEventArgs>? SessionCompleted;

    void Start();
    void Pause();
    void Reset();
    void Skip();
    void Tick(int secondsElapsed);
    void ApplySettings(UserSettings settings);
}

