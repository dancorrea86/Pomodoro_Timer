using PomodoroTimer.Domain.Enums;

namespace PomodoroTimer.Domain.Events;

public class PomodoroCompletedEventArgs : EventArgs
{
    public SessionType CompletedMode { get; }
    public SessionType NextMode { get; }
    public int FocusCycleCount { get; }

    public PomodoroCompletedEventArgs(SessionType completedMode, SessionType nextMode, int focusCycleCount)
    {
        CompletedMode = completedMode;
        NextMode = nextMode;
        FocusCycleCount = focusCycleCount;
    }
}
