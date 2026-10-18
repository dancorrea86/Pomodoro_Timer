using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Enums;
using PomodoroTimer.Domain.Interfaces;

namespace PomodoroTimer.Services;

public class PomodoroEngine : IPomodoroEngine
{
    private readonly UserSettings _settings;

    public PomodoroEngine() : this(new UserSettings())
    {
    }

    public PomodoroEngine(UserSettings? settings)
    {
        _settings = settings ?? new UserSettings();
        CurrentMode = SessionType.Focus;
        RemainingSecondsInternal = GetDurationFor(CurrentMode);
        State = TimerState.Stopped;
        FocusCycleCount = 0;
    }

    public SessionType CurrentMode { get; private set; }
    public int RemainingSeconds => RemainingSecondsInternal;
    public TimerState State { get; private set; }
    public int FocusCycleCount { get; private set; }

    private int RemainingSecondsInternal { get; set; }

    public void Start()
    {
        if (State == TimerState.Running)
        {
            return;
        }

        if (RemainingSecondsInternal <= 0)
        {
            RemainingSecondsInternal = GetDurationFor(CurrentMode);
        }

        State = TimerState.Running;
    }

    public void Pause()
    {
        if (State == TimerState.Running)
        {
            State = TimerState.Paused;
        }
    }

    public void Reset()
    {
        RemainingSecondsInternal = GetDurationFor(CurrentMode);
        State = TimerState.Paused;
    }

    public void Skip()
    {
        CurrentMode = GetNextMode(CurrentMode);
        RemainingSecondsInternal = GetDurationFor(CurrentMode);
        State = TimerState.Paused;
    }

    public void Tick(int secondsElapsed)
    {
        if (secondsElapsed <= 0 || State != TimerState.Running)
        {
            return;
        }

        RemainingSecondsInternal = Math.Max(0, RemainingSecondsInternal - secondsElapsed);

        if (RemainingSecondsInternal == 0)
        {
            AdvanceCycle();
        }
    }

    public void ApplySettings(UserSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        _settings.FocusDurationMinutes = settings.FocusDurationMinutes;
        _settings.ShortBreakMinutes = settings.ShortBreakMinutes;
        _settings.LongBreakMinutes = settings.LongBreakMinutes;
        _settings.CyclesBeforeLongBreak = settings.CyclesBeforeLongBreak;
        _settings.SoundEnabled = settings.SoundEnabled;

        if (State == TimerState.Stopped)
        {
            RemainingSecondsInternal = GetDurationFor(CurrentMode);
        }
    }

    private void AdvanceCycle()
    {
        if (CurrentMode == SessionType.Focus)
        {
            FocusCycleCount++;

            if (FocusCycleCount >= _settings.CyclesBeforeLongBreak)
            {
                FocusCycleCount = 0;
                CurrentMode = SessionType.LongBreak;
            }
            else
            {
                CurrentMode = SessionType.ShortBreak;
            }
        }
        else
        {
            CurrentMode = SessionType.Focus;
        }

        RemainingSecondsInternal = GetDurationFor(CurrentMode);
        State = TimerState.Paused;
    }

    private int GetDurationFor(SessionType mode)
    {
        return mode switch
        {
            SessionType.Focus => _settings.FocusDurationMinutes * 60,
            SessionType.ShortBreak => _settings.ShortBreakMinutes * 60,
            SessionType.LongBreak => _settings.LongBreakMinutes * 60,
            _ => 25 * 60
        };
    }

    private static SessionType GetNextMode(SessionType currentMode)
    {
        return currentMode switch
        {
            SessionType.Focus => SessionType.ShortBreak,
            SessionType.ShortBreak => SessionType.Focus,
            SessionType.LongBreak => SessionType.Focus,
            _ => SessionType.Focus
        };
    }
}
