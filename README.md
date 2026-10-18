# ⏱️ Pomodoro Timer Desktop

Aplicação desktop para gerenciamento de tempo e produtividade baseada na técnica Pomodoro, desenvolvida em **.NET 10 (Windows Forms)** e **C# 14**, seguindo rigorosamente a metodologia **Spec-Driven Development (SDD)**.

---

## 📋 Metodologia Spec-Driven Development

Todo o ciclo de vida deste projeto é guiado por especificações documentadas no diretório [`specs/`](specs/):

- 📜 **[Constitution (`specs/constitution.md`)](specs/constitution.md)**: Diretrizes arquiteturais, stack tecnológica, padrões de interface e convenções de código.
- 🎯 **[Functional Specification (`specs/spec.md`)](specs/spec.md)**: Requisitos funcionais, casos de uso (`UC-01` a `UC-04`), regras de negócio (`RN-01` a `RN-04`) e critérios de aceitação em BDD (Gherkin).
- 📐 **[Technical Plan (`specs/plan.md`)](specs/plan.md)**: Estrutura em camadas, mapeamento de banco de dados SQLite + EF Core, navegação por UserControls e máquina de estados do timer.
- ✅ **[Task Checklist (`specs/task.md`)](specs/task.md)**: Roadmap de implementação em 4 fases com status de execução de cada tarefa.

---

## 🏛️ Arquitetura da Solução

O projeto adota uma arquitetura em camadas clara em `src/`:

```text
src/
├── PomodoroTimer.Domain/       # Entidades, Enums e Interfaces centrais (sem dependências externas)
├── PomodoroTimer.Data/         # DbContext (EF Core), Migrations e persistência em SQLite
├── PomodoroTimer.Services/     # Regras de negócio, Pomodoro Engine, Autenticação e Tarefas
└── PomodoroTimer.UI/           # Aplicação Windows Forms (Forms, UserControls, DI Container)
```

### Regras de Dependência:
- `PomodoroTimer.Domain` não depende de nenhuma camada.
- `PomodoroTimer.Data` depende de `PomodoroTimer.Domain`.
- `PomodoroTimer.Services` depende de `PomodoroTimer.Domain`.
- `PomodoroTimer.UI` integra todas as camadas (`Domain`, `Data`, `Services`) via Injeção de Dependência.

---

## 🛠️ Stack Tecnológica

- **Linguagem:** C# 14 (.NET 10 SDK)
- **Interface:** Windows Forms (WinForms) moderna com layout modular via UserControls
- **Persistência:** SQLite via Entity Framework Core 10
- **Injeção de Dependência:** `Microsoft.Extensions.DependencyInjection`
- **Segurança:** Hashing seguro de senhas por usuário (PBKDF2 / BCrypt)

---

## 🚀 Como Executar

### Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Sistema Operacional: Windows 10/11

### Compilação e Execução

Clone o repositório e acesse o diretório raiz:

```bash
# Restaurar dependências
dotnet restore

# Compilar a solução
dotnet build

# Executar a aplicação Windows Forms
dotnet run --project src/PomodoroTimer.UI
```

---

## 📅 Roadmap e Status das Tarefas

- [x] **Fase 1: Setup & Data Layer**
  - [x] `T-101`: Inicialização da solução .NET 10 e criação dos projetos em `src/`.
  - [x] `T-102`: Instalação de pacotes NuGet (EF Core, SQLite, DI).
  - [x] `T-103`: Criação das entidades no `Domain`.
  - [x] `T-104`: Configuração do `PomodoroDbContext` no `Data`.
  - [x] `T-105`: Criação das Migrations iniciais do SQLite.
- [ ] **Fase 2: Core Services & Logic** (Autenticação, Engine do Timer, Gerenciamento de Tarefas e Testes Unitários)
- [ ] **Fase 3: UI Development** (Telas de Login, Timer, Configurações e Navegação)
- [ ] **Fase 4: Polish & Integration** (Tema Dark, Validação e Testes E2E)
