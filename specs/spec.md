# Functional Specification: Pomodoro Timer Desktop App

## 1. Feature Specifications

### 1.1 Authentication & User Session (`LoginUserControl`)
- **UC-01 (Login/Register & Session Lifecycle):**
  - Users can switch between "Login" and "Register" mode within the same screen.
  - Required fields: Username and Password.
  - Validation: Passwords must be at least 6 characters. Username must be unique.
  - On successful login, save user session context and load `TimerUserControl`.
  - **Session Persistence:** The user session persists across app restarts (the app automatically reopens with the last authenticated user).
  - **Logout:** Provide a clear "Logout" action in the interface that clears the stored user session and redirects back to `LoginUserControl`.

### 1.2 Pomodoro Timer & Task Execution (`TimerUserControl`)
- **UC-02 (Timer Display & Controls):**
  - Show countdown formatted as `MM:SS`.
  - Action buttons:
    - `Start`: Starts or resumes the countdown for the current mode.
    - `Pause`: Pauses the countdown at the current elapsed time.
    - `Skip`: Immediately interrupts the current session without saving completion or incrementing metrics, advancing directly to the next mode in a **paused** state.
    - `Reset`: Restores the current mode's timer to its initial full duration in a paused state without saving a session.
  - Visual indicator showing current mode: **Focus**, **Short Break**, or **Long Break**.
  - Sound notification on session completion (when timer reaches 00:00).
  - **Mode Transitions:** Upon session completion, the timer automatically transitions to the next mode in a **paused** state (requires manual `Start` to begin the next countdown).
- **UC-03 (Task Management):**
  - Create, view, complete, and delete tasks.
  - **Task Association:** Associating an active Pomodoro session with a specific task is **optional**. Users can run timers without selecting any task.
  - When a Focus session completes with a selected task, increment the `CompletedPomodoros` count on that task.
  - **Session Logging:** Persist session logs to `PomodoroSession` table upon completion (00:00) for **all modes** (**Focus**, **Short Break**, **Long Break**), recording session type, duration, completion timestamp, and optional associated task.

### 1.3 User Settings (`SettingsUserControl`)
- **UC-04 (Configuration Customization):**
  - Customize durations (in minutes) with initial defaults:
    - **Focus Time:** Default 25 minutes (valid range: 1–60 integer minutes).
    - **Short Break Time:** Default 5 minutes (valid range: 1–60 integer minutes).
    - **Long Break Time:** Default 15 minutes (valid range: 1–60 integer minutes).
  - Set `CyclesBeforeLongBreak`: Default 4 cycles (valid range: integer >= 1).
  - Toggle sound notifications on/off.
  - Save changes directly to SQLite via `UserSettings` table.

## 2. Business Rules (RN)
- **RN-01:** A `PomodoroSession` log entry is only saved as completed if the timer reaches `00:00` without being reset or skipped. All completed modes (**Focus**, **Short Break**, **Long Break**) are persisted in `PomodoroSession` with their respective type.
- **RN-02:** Switching views while a timer is active must keep the timer running in the background.
- **RN-03:** Users cannot access Timer or Settings screens without authenticating first.
- **RN-04:** Uncompleted tasks remain listed; completed tasks move to an optional "Completed" view filter.
- **RN-05:** Activating the `Skip` button interrupts the current session, does not record an entry in `PomodoroSession`, does not increment `CompletedPomodoros`, does not advance the completed cycle count toward `CyclesBeforeLongBreak`, and sets the timer to the next mode in a paused state.
- **RN-06:** Mode transition and cycle tracking follow these rules:
  - After completing a **Focus** session, increment the focus cycle count. If the count reaches `CyclesBeforeLongBreak`, transition to **Long Break** and reset the focus cycle counter to 0; otherwise, transition to **Short Break**.
  - After completing a **Short Break** or **Long Break**, transition back to **Focus**.
  - All automatic transitions place the timer in a **paused** state awaiting user confirmation (`Start`).
- **RN-07:** Task association is optional. If no task is selected when a Focus session completes, the session is saved in `PomodoroSession` without a task foreign key, and general focus time is tracked.
- **RN-08:** User session state is persisted locally so that relaunching the application restores the authenticated session. Logging out explicitly clears the stored session and returns to `LoginUserControl`.

## 3. Acceptance Criteria (BDD)

```gherkin
Scenario: Completing a Focus Session (Normal Cycle - Transition to Short Break)
  Given the timer is running in Focus mode for Task "Write Specs"
  And the current focus cycle count is less than CyclesBeforeLongBreak minus 1
  When the timer reaches 00:00
  Then play completion sound (if enabled)
  And record a new PomodoroSession entry in SQLite with type "Focus" and Task "Write Specs"
  And increment CompletedPomodoros on "Write Specs"
  And increment the focus cycle count by 1
  And automatically transition to Short Break mode in a paused state

Scenario: Completing a Focus Session reaching CyclesBeforeLongBreak (Transition to Long Break)
  Given the timer is running in Focus mode
  And the current focus cycle count reaches CyclesBeforeLongBreak minus 1
  When the timer reaches 00:00
  Then play completion sound (if enabled)
  And record a new PomodoroSession entry in SQLite with type "Focus"
  And reset the focus cycle count to 0
  And automatically transition to Long Break mode in a paused state

Scenario: Completing a Break Session
  Given the timer is running in Short Break mode
  When the timer reaches 00:00
  Then play completion sound (if enabled)
  And record a new PomodoroSession entry in SQLite with type "Short Break"
  And automatically transition to Focus mode in a paused state

Scenario: Skipping an Active Session
  Given the timer is running in Focus mode for Task "Write Specs" with remaining time "14:20"
  When the user clicks "Skip"
  Then do not record a PomodoroSession entry in SQLite
  And do not increment CompletedPomodoros on "Write Specs"
  And do not increment the focus cycle count
  And transition to the next mode in a paused state

Scenario: Completing a Focus Session without a Task
  Given the timer is running in Focus mode with no task selected
  When the timer reaches 00:00
  Then play completion sound (if enabled)
  And record a new PomodoroSession entry in SQLite with type "Focus" and no associated task
  And automatically transition to Short Break mode in a paused state
```