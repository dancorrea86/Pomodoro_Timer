# Functional Specification: Pomodoro Timer Desktop App

## 1. Feature Specifications

### 1.1 Authentication & User Session (`LoginUserControl`)
- **UC-01 (Login/Register):**
  - Users can switch between "Login" and "Register" mode within the same screen.
  - Required fields: Username and Password.
  - Validation: Passwords must be at least 6 characters. Username must be unique.
  - On successful login, save user session context and load `TimerUserControl`.

### 1.2 Pomodoro Timer & Task Execution (`TimerUserControl`)
- **UC-02 (Timer Display & Controls):**
  - Show countdown formatted as `MM:SS`.
  - Action buttons: `Start`, `Pause`, `Skip`, `Reset`.
  - Visual indicator showing current mode: **Focus**, **Short Break**, or **Long Break**.
  - Sound notification on session completion.
- **UC-03 (Task Management):**
  - Create, view, complete, and delete tasks.
  - Associate the active Pomodoro session with a specific task.
  - Increment `CompletedPomodoros` count on task when a Focus session completes.
  - Persist session logs to `PomodoroSession` table upon completion.

### 1.3 User Settings (`SettingsUserControl`)
- **UC-04 (Configuration Customization):**
  - Customize durations (in minutes): Focus Time, Short Break Time, Long Break Time.
  - Set `CyclesBeforeLongBreak` (default: 4).
  - Toggle sound notifications on/off.
  - Save changes directly to SQLite via `UserSettings` table.

## 2. Business Rules (RN)
- **RN-01:** A `PomodoroSession` is only saved as completed if the timer reaches 00:00 without being reset.
- **RN-02:** Switching views while a timer is active must keep the timer running in the background.
- **RN-03:** Users cannot access Timer or Settings screens without authenticating first.
- **RN-04:** Uncompleted tasks remain listed; completed tasks move to an optional "Completed" view filter.

## 3. Acceptance Criteria (BDD)

```gherkin
Scenario: Completing a Focus Session
  Given the timer is running in Focus mode for Task "Write Specs"
  When the timer reaches 00:00
  Then play completion sound (if enabled)
  And record a new PomodoroSession entry in SQLite
  And increment CompletedPomodoros on "Write Specs"
  And automatically transition to Short Break mode
```