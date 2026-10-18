# Constitution: Pomodoro Timer Desktop App

## 1. Visão Geral & Princípios do Projeto
Esta constituição estabelece os princípios arquiteturais, convenções de código e diretrizes técnicas obrigatórias para o projeto **Pomodoro Timer Desktop**. O objetivo é garantir um código sustentável, modular, testável e com uma interface moderna em Windows Forms.

---

## 2. Stack Tecnológica Base
- **Plataforma Target:** .NET 10 (Windows Forms / WinForms)
- **Linguagem:** C# 14 (ou versão estável mais recente do .NET 10 SDK)
- **Persistência de Dados:** SQLite
- **ORM:** Entity Framework Core 10 (EF Core) com suporte a Migrations
- **Injeção de Dependência:** `Microsoft.Extensions.DependencyInjection`
- **Logging:** Serilog ou `Microsoft.Extensions.Logging` (estruturado em arquivo local/SQLite)

---

## 3. Diretrizes de Arquitetura & Organização em `src/`

O projeto deve seguir uma arquitetura em camadas clara ou Clean Architecture simplificada dentro da solução C#:

```text
src/
├── PomodoroTimer.Domain/       # Entidades, Enums e Interfaces fundamentais
├── PomodoroTimer.Data/         # DbContext do EF Core, Migrations e Repositórios
├── PomodoroTimer.Services/     # Regras de negócio (Pomodoro Engine, Autenticação, Relatórios)
└── PomodoroTimer.UI/           # Aplicação WinForms (Views, Forms, UserControls, Presenters/ViewModels)
```

### Regras de Dependência
1. `Domain` não depende de nenhuma outra camada.
2. `Data` e `Services` dependem apenas de `Domain`.
3. `UI` depende de `Services`, `Data` (para configuração de DI/DbContext) e `Domain`.
4. É estritamente proibido colocar lógica de negócio diretamente nos eventos de clique/UI dos Forms (`Form1.cs`).

---

## 4. Padrões de Interface do Usuário (WinForms UI)

A aplicação possuirá 3 telas principais com navegação limpa e fluida:

1. **Tela de Login (`LoginForm`)**:
   - Autenticação de usuário com opção de persistir sessão/credencial no SQLite.
   - Design limpo com foco em usabilidade.

2. **Tela do Timer Pomodoro (`MainTimerForm`)**:
   - Exibição visual e contagem regressiva do tempo (Foco / Pausa Curta / Pausa Longa).
   - Seleção e cadastro rápido de tarefas associadas ao ciclo Pomodoro ativo.
   - Notificação sonora e/ou visual ao término de cada sessão.

3. **Tela de Configurações (`SettingsForm`)**:
   - Personalização de tempos (Duração do Foco, Pausa Curta, Pausa Longa, Ciclos para Pausa Longa).
   - Preferências de áudio, tema visual e opções de perfil do usuário.

### Diretrizes de UX/UI
- **Mapeamento de Cores & Estilo:** Utilizar paleta de cores moderna (Dark Mode nativo ou cores limpas personalizadas com custom rendering via `Paint` / `UserControls`).
- **Navegação Modular:** Alternar entre telas utilizando um container central (`Panel`) ou gerenciador de navegação baseado em `UserControl`, evitando abrir múltiplos pop-ups desnecessários.
- **Responsividade:** Componentes devem usar ancoragem (`Anchor`) e alinhamento (`Dock`) adequados para suportar redimensionamento limpo.

---

## 5. Modelo de Dados & Persistência (EF Core + SQLite)

### Entidades Principais
- **`User`**: `Id`, `Username`, `PasswordHash`, `Salt`, `CreatedAt`, `SettingsId`
- **`UserSettings`**: `Id`, `UserId`, `FocusDurationMinutes`, `ShortBreakMinutes`, `LongBreakMinutes`, `CyclesBeforeLongBreak`, `SoundEnabled`
- **`TaskItem`**: `Id`, `UserId`, `Title`, `Description`, `IsCompleted`, `EstimatedPomodoros`, `CompletedPomodoros`, `CreatedAt`
- **`PomodoroSession`**: `Id`, `TaskId`, `UserId`, `StartTime`, `EndTime`, `DurationMinutes`, `Type` (Enum: Foco, PausaCurta, PausaLonga), `IsCompleted`

### Regras do EF Core
- Migrations ativas na inicialização ou aplicadas via código no `Program.cs` (`dbContext.Database.Migrate()`).
- O banco SQLite deve ser armazenado na pasta `%AppData%` ou local do aplicativo (`pomodoro.db`).
- Senhas de usuário **nunca** devem ser salvas em texto puro. Deve ser utilizado algoritmo seguro de hashing (ex: `PasswordHasher` do ASP.NET Core Identity ou `BCrypt` / `Argon2`).

---

## 6. Convenções de Código C#

- **Nomenclatura:**
  - Interfaces: Prefixo `I` (ex: `IPomodoroTimerService`).
  - Métodos Assíncronos: Sufixo `Async` (ex: `SaveTaskAsync`).
  - Eventos: Usar convenção padrão C# (`OnTimerTick`, `PomodoroCompletedEventArgs`).
- **Tratamento de Exceções:** Lógica de retry para persistência e captura global de exceções não tratadas via `Application.ThreadException`.
- **Injeção de Dependência:** Todos os serviços, DbContext e Forms/UserControls devem ser registrados no container `IServiceCollection`.

---

## 7. Garantia de Qualidade & Testes

- Regras de cálculo do Pomodoro (ex: transição automática Foco -> Pausa -> Pausa Longa) devem ter testes unitários cobrindo o `PomodoroTimerService`.
- Consultas EF Core devem ser otimizadas com `AsNoTracking()` para leituras sem rastreamento de estado.