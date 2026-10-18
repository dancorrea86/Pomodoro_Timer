using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Enums;
using PomodoroTimer.Services;

namespace PomodoroTimer.Tests;

public class PomodoroEngineTests
{
    [Fact]
    public void Constructor_ShouldStartInFocusMode_WithDefaultDuration()
    {
        var engine = new PomodoroEngine();

        Assert.Equal(SessionType.Focus, engine.CurrentMode);
        Assert.Equal(25 * 60, engine.RemainingSeconds);
        Assert.Equal(TimerState.Stopped, engine.State);
    }

    [Fact]
    public void Start_ShouldChangeStateToRunning()
    {
        var engine = new PomodoroEngine();

        engine.Start();

        Assert.Equal(TimerState.Running, engine.State);
    }

    [Fact]
    public void Tick_ShouldAdvanceCountdown_AndCompleteFocusCycle_ToShortBreak()
    {
        var engine = new PomodoroEngine(new UserSettings { FocusDurationMinutes = 25, ShortBreakMinutes = 5, LongBreakMinutes = 15, CyclesBeforeLongBreak = 4 });
        engine.Start();

        engine.Tick(25 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);
        Assert.Equal(5 * 60, engine.RemainingSeconds);
        Assert.Equal(1, engine.FocusCycleCount);
    }

    [Fact]
    public void Tick_ShouldTransitionToLongBreak_WhenFocusCycleLimitIsReached()
    {
        var engine = new PomodoroEngine(new UserSettings { FocusDurationMinutes = 25, ShortBreakMinutes = 5, LongBreakMinutes = 15, CyclesBeforeLongBreak = 1 });

        engine.Start();
        engine.Tick(25 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.LongBreak, engine.CurrentMode);
        Assert.Equal(15 * 60, engine.RemainingSeconds);
        Assert.Equal(0, engine.FocusCycleCount);
    }

    [Fact]
    public void Skip_ShouldMoveToNextMode_WithoutChangingCycleCount()
    {
        var engine = new PomodoroEngine(new UserSettings { FocusDurationMinutes = 25, ShortBreakMinutes = 5, LongBreakMinutes = 15, CyclesBeforeLongBreak = 4 });
        engine.Start();
        engine.Tick(10 * 60);

        engine.Skip();

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);
        Assert.Equal(5 * 60, engine.RemainingSeconds);
        Assert.Equal(0, engine.FocusCycleCount);
    }

    [Fact]
    public void Reset_ShouldRestoreCurrentModeDuration_AndPauseTheTimer()
    {
        var engine = new PomodoroEngine(new UserSettings { FocusDurationMinutes = 25, ShortBreakMinutes = 5, LongBreakMinutes = 15, CyclesBeforeLongBreak = 4 });
        engine.Start();
        engine.Tick(60);

        engine.Reset();

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(25 * 60, engine.RemainingSeconds);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
    }
}
