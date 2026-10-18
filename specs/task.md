# Task Checklist: Pomodoro Timer Desktop App

## Phase 1: Setup & Data Layer
- [ ] **T-101:** Initialize .NET 10 WinForms solution and create projects in `src/`.
- [ ] **T-102:** Install NuGet packages: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.Extensions.DependencyInjection`.
- [ ] **T-103:** Implement `Domain` entities (`User`, `UserSettings`, `TaskItem`, `PomodoroSession`).
- [ ] **T-104:** Create `PomodoroDbContext` in `PomodoroTimer.Data` with Fluent API mappings.
- [ ] **T-105:** Create initial EF Core Migration (`InitialCreate`) and test SQLite database generation.

## Phase 2: Core Services & Logic
- [ ] **T-201:** Implement `AuthService` with password hashing (PBKDF2/BCrypt) and register logic.
- [ ] **T-202:** Implement `PomodoroEngine` service handling state machine (Start, Pause, Reset, Tick, State Transition).
- [ ] **T-203:** Implement `TaskService` for CRUD operations on tasks and session linking.
- [ ] **T-204:** Write unit tests for `PomodoroEngine` cycle transitions (Focus -> Short Break -> Long Break).

## Phase 3: UI Development (WinForms Modern Design)
- [ ] **T-301:** Configure DI Container in `Program.cs` to inject DbContext, Services, and Views.
- [ ] **T-302:** Design `MainForm` container shell with dynamic view switcher panel.
- [ ] **T-303:** Build `LoginUserControl` (login, register form, input validation).
- [ ] **T-304:** Build `TimerUserControl` (digital countdown display, play/pause controls, task dropdown).
- [ ] **T-305:** Build `SettingsUserControl` (duration inputs, toggles, save action).
- [ ] **T-306:** Implement audio notification system using `System.Media.SoundPlayer`.

## Phase 4: Polish & Integration
- [ ] **T-401:** Apply cohesive Dark Theme color palette and UI custom styling.
- [ ] **T-402:** Validate session persistence across app restarts.
- [ ] **T-403:** End-to-end testing of user login -> task creation -> timer run -> settings update.