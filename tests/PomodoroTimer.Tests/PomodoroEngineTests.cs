using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Enums;
using PomodoroTimer.Domain.Events;
using PomodoroTimer.Services;
using Xunit;

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
        Assert.Equal(0, engine.FocusCycleCount);
    }

    [Fact]
    public void Start_ShouldChangeStateToRunning()
    {
        var engine = new PomodoroEngine();

        engine.Start();

        Assert.Equal(TimerState.Running, engine.State);
    }

    [Fact]
    public void Pause_ShouldChangeStateToPaused_WhenRunning()
    {
        var engine = new PomodoroEngine();
        engine.Start();

        engine.Pause();

        Assert.Equal(TimerState.Paused, engine.State);
    }

    [Fact]
    public void Tick_ShouldAdvanceCountdown_AndCompleteFocusCycle_ToShortBreak()
    {
        var engine = new PomodoroEngine(new UserSettings
        {
            FocusDurationMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 4
        });
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
        var engine = new PomodoroEngine(new UserSettings
        {
            FocusDurationMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 1
        });

        engine.Start();
        engine.Tick(25 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.LongBreak, engine.CurrentMode);
        Assert.Equal(15 * 60, engine.RemainingSeconds);
        Assert.Equal(0, engine.FocusCycleCount);
    }

    [Fact]
    public void FullFourCycleWorkflow_ShouldTransitionFocus_ToShortBreaks_AndLongBreak_AndBackToFocus()
    {
        // 4 Focus cycles: Focus -> ShortBreak -> Focus -> ShortBreak -> Focus -> ShortBreak -> Focus -> LongBreak -> Focus
        var settings = new UserSettings
        {
            FocusDurationMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 4
        };
        var engine = new PomodoroEngine(settings);

        var completedEvents = new List<PomodoroCompletedEventArgs>();
        engine.SessionCompleted += (_, e) => completedEvents.Add(e);

        // --- Cycle 1: Focus 1 ---
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
        Assert.Equal(0, engine.FocusCycleCount);
        engine.Start();
        engine.Tick(25 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);
        Assert.Equal(5 * 60, engine.RemainingSeconds);
        Assert.Equal(1, engine.FocusCycleCount);

        // Short Break 1 finishes -> Focus 2
        engine.Start();
        engine.Tick(5 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
        Assert.Equal(25 * 60, engine.RemainingSeconds);
        Assert.Equal(1, engine.FocusCycleCount); // Count preserved during break

        // --- Cycle 2: Focus 2 ---
        engine.Start();
        engine.Tick(25 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);
        Assert.Equal(5 * 60, engine.RemainingSeconds);
        Assert.Equal(2, engine.FocusCycleCount);

        // Short Break 2 finishes -> Focus 3
        engine.Start();
        engine.Tick(5 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
        Assert.Equal(25 * 60, engine.RemainingSeconds);
        Assert.Equal(2, engine.FocusCycleCount);

        // --- Cycle 3: Focus 3 ---
        engine.Start();
        engine.Tick(25 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);
        Assert.Equal(5 * 60, engine.RemainingSeconds);
        Assert.Equal(3, engine.FocusCycleCount);

        // Short Break 3 finishes -> Focus 4
        engine.Start();
        engine.Tick(5 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
        Assert.Equal(25 * 60, engine.RemainingSeconds);
        Assert.Equal(3, engine.FocusCycleCount);

        // --- Cycle 4: Focus 4 (reaching CyclesBeforeLongBreak = 4) ---
        engine.Start();
        engine.Tick(25 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.LongBreak, engine.CurrentMode); // 4th focus completed -> Long Break!
        Assert.Equal(15 * 60, engine.RemainingSeconds);
        Assert.Equal(0, engine.FocusCycleCount); // Reset to 0

        // Long Break finishes -> Focus 1 (new cycle loop)
        engine.Start();
        engine.Tick(15 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
        Assert.Equal(25 * 60, engine.RemainingSeconds);
        Assert.Equal(0, engine.FocusCycleCount);

        // Verify all 8 transitions raised SessionCompleted events in order
        Assert.Equal(8, completedEvents.Count);
        Assert.Equal((SessionType.Focus, SessionType.ShortBreak, 1), (completedEvents[0].CompletedMode, completedEvents[0].NextMode, completedEvents[0].FocusCycleCount));
        Assert.Equal((SessionType.ShortBreak, SessionType.Focus, 1), (completedEvents[1].CompletedMode, completedEvents[1].NextMode, completedEvents[1].FocusCycleCount));
        Assert.Equal((SessionType.Focus, SessionType.ShortBreak, 2), (completedEvents[2].CompletedMode, completedEvents[2].NextMode, completedEvents[2].FocusCycleCount));
        Assert.Equal((SessionType.ShortBreak, SessionType.Focus, 2), (completedEvents[3].CompletedMode, completedEvents[3].NextMode, completedEvents[3].FocusCycleCount));
        Assert.Equal((SessionType.Focus, SessionType.ShortBreak, 3), (completedEvents[4].CompletedMode, completedEvents[4].NextMode, completedEvents[4].FocusCycleCount));
        Assert.Equal((SessionType.ShortBreak, SessionType.Focus, 3), (completedEvents[5].CompletedMode, completedEvents[5].NextMode, completedEvents[5].FocusCycleCount));
        Assert.Equal((SessionType.Focus, SessionType.LongBreak, 0), (completedEvents[6].CompletedMode, completedEvents[6].NextMode, completedEvents[6].FocusCycleCount));
        Assert.Equal((SessionType.LongBreak, SessionType.Focus, 0), (completedEvents[7].CompletedMode, completedEvents[7].NextMode, completedEvents[7].FocusCycleCount));
    }

    [Fact]
    public void TwoCyclesWorkflow_ShouldTransitionToLongBreak_AfterTwoFocusSessions()
    {
        var settings = new UserSettings
        {
            FocusDurationMinutes = 20,
            ShortBreakMinutes = 3,
            LongBreakMinutes = 10,
            CyclesBeforeLongBreak = 2
        };
        var engine = new PomodoroEngine(settings);

        // Focus 1
        engine.Start();
        engine.Tick(20 * 60);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);
        Assert.Equal(3 * 60, engine.RemainingSeconds);
        Assert.Equal(1, engine.FocusCycleCount);

        // Short Break 1
        engine.Start();
        engine.Tick(3 * 60);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
        Assert.Equal(20 * 60, engine.RemainingSeconds);
        Assert.Equal(1, engine.FocusCycleCount);

        // Focus 2 (Limit reached)
        engine.Start();
        engine.Tick(20 * 60);
        Assert.Equal(SessionType.LongBreak, engine.CurrentMode);
        Assert.Equal(10 * 60, engine.RemainingSeconds);
        Assert.Equal(0, engine.FocusCycleCount);

        // Long Break
        engine.Start();
        engine.Tick(10 * 60);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
        Assert.Equal(20 * 60, engine.RemainingSeconds);
        Assert.Equal(0, engine.FocusCycleCount);
    }

    [Fact]
    public void ShortBreak_ShouldTransitionBackToFocus_InPausedState()
    {
        var engine = new PomodoroEngine(new UserSettings
        {
            FocusDurationMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 4
        });

        // Focus completes
        engine.Start();
        engine.Tick(25 * 60);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);

        // Short break completes
        engine.Start();
        engine.Tick(5 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
        Assert.Equal(25 * 60, engine.RemainingSeconds);
    }

    [Fact]
    public void LongBreak_ShouldTransitionBackToFocus_InPausedState_WithCycleCountZero()
    {
        var engine = new PomodoroEngine(new UserSettings
        {
            FocusDurationMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 1
        });

        // Focus completes -> Long Break
        engine.Start();
        engine.Tick(25 * 60);
        Assert.Equal(SessionType.LongBreak, engine.CurrentMode);

        // Long break completes
        engine.Start();
        engine.Tick(15 * 60);

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
        Assert.Equal(25 * 60, engine.RemainingSeconds);
        Assert.Equal(0, engine.FocusCycleCount);
    }

    [Fact]
    public void CustomDurations_ShouldBeAppliedCorrectly_AcrossTransitions()
    {
        var engine = new PomodoroEngine(new UserSettings
        {
            FocusDurationMinutes = 50,
            ShortBreakMinutes = 10,
            LongBreakMinutes = 30,
            CyclesBeforeLongBreak = 2
        });

        Assert.Equal(50 * 60, engine.RemainingSeconds);

        // Focus
        engine.Start();
        engine.Tick(50 * 60);
        Assert.Equal(10 * 60, engine.RemainingSeconds);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);

        // Short break
        engine.Start();
        engine.Tick(10 * 60);
        Assert.Equal(50 * 60, engine.RemainingSeconds);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);

        // Focus 2
        engine.Start();
        engine.Tick(50 * 60);
        Assert.Equal(30 * 60, engine.RemainingSeconds);
        Assert.Equal(SessionType.LongBreak, engine.CurrentMode);
    }

    [Fact]
    public void Skip_FromFocus_ShouldMoveToShortBreak_WithoutIncrementingCycleCount()
    {
        var engine = new PomodoroEngine(new UserSettings
        {
            FocusDurationMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 4
        });
        engine.Start();
        engine.Tick(10 * 60);

        engine.Skip();

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);
        Assert.Equal(5 * 60, engine.RemainingSeconds);
        Assert.Equal(0, engine.FocusCycleCount);
    }

    [Fact]
    public void Skip_FromShortBreak_ShouldMoveToFocus_WithoutChangingCycleCount()
    {
        var engine = new PomodoroEngine(new UserSettings
        {
            FocusDurationMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 4
        });

        // Advance to ShortBreak
        engine.Start();
        engine.Tick(25 * 60);
        Assert.Equal(1, engine.FocusCycleCount);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);

        // Skip Short Break
        engine.Skip();

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
        Assert.Equal(25 * 60, engine.RemainingSeconds);
        Assert.Equal(1, engine.FocusCycleCount); // Cycle count remains 1
    }

    [Fact]
    public void Skip_FromLongBreak_ShouldMoveToFocus_WithoutChangingCycleCount()
    {
        var engine = new PomodoroEngine(new UserSettings
        {
            FocusDurationMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 1
        });

        // Advance to LongBreak
        engine.Start();
        engine.Tick(25 * 60);
        Assert.Equal(SessionType.LongBreak, engine.CurrentMode);
        Assert.Equal(0, engine.FocusCycleCount);

        // Skip Long Break
        engine.Skip();

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
        Assert.Equal(25 * 60, engine.RemainingSeconds);
        Assert.Equal(0, engine.FocusCycleCount);
    }

    [Fact]
    public void Reset_ShouldRestoreCurrentModeDuration_AndPauseTheTimer()
    {
        var engine = new PomodoroEngine(new UserSettings
        {
            FocusDurationMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 4
        });
        engine.Start();
        engine.Tick(60);

        engine.Reset();

        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(25 * 60, engine.RemainingSeconds);
        Assert.Equal(SessionType.Focus, engine.CurrentMode);
    }

    [Fact]
    public void Reset_DuringBreak_ShouldRestoreBreakDuration()
    {
        var engine = new PomodoroEngine(new UserSettings
        {
            FocusDurationMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 4
        });

        // Move to ShortBreak
        engine.Start();
        engine.Tick(25 * 60);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);

        // Tick 30 seconds
        engine.Start();
        engine.Tick(30);
        Assert.Equal(5 * 60 - 30, engine.RemainingSeconds);

        // Reset
        engine.Reset();
        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(5 * 60, engine.RemainingSeconds);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);
    }

    [Fact]
    public void StepByStepTick_ShouldTransitionExactlyAtZero()
    {
        var engine = new PomodoroEngine(new UserSettings
        {
            FocusDurationMinutes = 1,
            ShortBreakMinutes = 1,
            LongBreakMinutes = 2,
            CyclesBeforeLongBreak = 2
        });

        engine.Start();

        // Tick 59 times 1 second
        for (int i = 0; i < 59; i++)
        {
            engine.Tick(1);
            Assert.Equal(SessionType.Focus, engine.CurrentMode);
            Assert.Equal(TimerState.Running, engine.State);
            Assert.Equal(60 - (i + 1), engine.RemainingSeconds);
        }

        // 60th tick hits 00:00 -> transition to ShortBreak in Paused state
        engine.Tick(1);
        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);
        Assert.Equal(TimerState.Paused, engine.State);
        Assert.Equal(60, engine.RemainingSeconds);
        Assert.Equal(1, engine.FocusCycleCount);
    }

    [Fact]
    public void Tick_ExceedingRemainingSeconds_ShouldNotUnderflow_AndTransitionCleanly()
    {
        var engine = new PomodoroEngine(new UserSettings
        {
            FocusDurationMinutes = 1,
            ShortBreakMinutes = 1,
            LongBreakMinutes = 2,
            CyclesBeforeLongBreak = 2
        });

        engine.Start();
        // 60s total, tick 90s
        engine.Tick(90);

        Assert.Equal(SessionType.ShortBreak, engine.CurrentMode);
        Assert.Equal(60, engine.RemainingSeconds);
        Assert.Equal(TimerState.Paused, engine.State);
    }

    [Fact]
    public void Tick_WhenPausedOrStopped_ShouldNotAdvanceCountdown()
    {
        var engine = new PomodoroEngine();

        // Stopped
        engine.Tick(10);
        Assert.Equal(25 * 60, engine.RemainingSeconds);

        // Paused
        engine.Start();
        engine.Pause();
        engine.Tick(10);
        Assert.Equal(25 * 60, engine.RemainingSeconds);
    }

    [Fact]
    public void ApplySettings_WhenStopped_ShouldUpdateRemainingSeconds()
    {
        var engine = new PomodoroEngine();
        Assert.Equal(25 * 60, engine.RemainingSeconds);

        engine.ApplySettings(new UserSettings
        {
            FocusDurationMinutes = 45,
            ShortBreakMinutes = 10,
            LongBreakMinutes = 20,
            CyclesBeforeLongBreak = 3
        });

        Assert.Equal(45 * 60, engine.RemainingSeconds);
    }

    [Fact]
    public void ApplySettings_Throws_WhenSettingsNull()
    {
        var engine = new PomodoroEngine();
        Assert.Throws<ArgumentNullException>(() => engine.ApplySettings(null!));
    }
}
