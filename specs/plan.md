# Technical Plan: Pomodoro Timer Desktop App

## 1. Context & Architecture Overview
This document describes the technical architecture and structural execution strategy for the Pomodoro Timer WinForms application built with .NET 10, Entity Framework Core, and SQLite.

## 2. Project Structure Mapping (`src/`)
The solution follows a 4-project layered structure inside `src/`:

```text
src/
├── PomodoroTimer.Domain/
│   ├── Entities/ (User, UserSettings, TaskItem, PomodoroSession)
│   ├── Enums/ (SessionType)
│   └── Interfaces/ (IRepository, IPomodoroEngine, IAuthService)
│
├── PomodoroTimer.Data/
│   ├── Context/ (PomodoroDbContext)
│   ├── Configurations/ (EF Core Fluent API mappings)
│   └── Migrations/ (Database schema versions)
│
├── PomodoroTimer.Services/
│   ├── AuthService.cs (Password hashing, login/session management)
│   ├── PomodoroEngine.cs (Timer state machine, cycle handling)
│   └── TaskService.cs (CRUD for user tasks and session tracking)
│
└── PomodoroTimer.UI/
    ├── Forms/ (MainForm, LoginForm)
    ├── Views/ (LoginUserControl, TimerUserControl, SettingsUserControl)
    ├── Components/ (Custom styled buttons, rounded panels, timer display)
    └── Program.cs (DI Container setup, Migration trigger, App Entry)
```

## 3. Database Schema & Persistence Strategy (SQLite + EF Core)

### Relationship Diagram
- `User` 1 --- 1 `UserSettings`
- `User` 1 --- N `TaskItem`
- `TaskItem` 1 --- N `PomodoroSession`

### Key Technical Decisions
1. **SQLite Database Path:** `%AppData%/PomodoroTimer/pomodoro.db`
2. **Auto Migration:** Execute `DbContext.Database.Migrate()` on application start.
3. **Password Security:** Use `Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2` (or `BCrypt.Net-Next`) with unique per-user salts.

## 4. UI Architecture & View Switching
- **Single Host Window Pattern:** `MainForm` acts as a shell with a top navigation bar and a central `ContentPanel`.
- Views (`LoginUserControl`, `TimerUserControl`, `SettingsUserControl`) are loaded dynamically into `ContentPanel` to ensure smooth transitions without flickering.
- UI Styling will use custom `UserControls` with `OnPaint` overrides for rounded borders, modern dark theme palettes, and clean timer progress rings.

## 5. Pomodoro State Machine Design
The `PomodoroEngine` service maintains state via a timer thread (`System.Threading.Timer` or `System.Windows.Forms.Timer` dispatched safely to UI thread):
- **States:** `Stopped`, `Running`, `Paused`
- **Session Types:** `Focus` (25 min default), `ShortBreak` (5 min default), `LongBreak` (15 min default)
- **Cycle Logic:** After 4 `Focus` sessions, automatically switch next break to `LongBreak`.