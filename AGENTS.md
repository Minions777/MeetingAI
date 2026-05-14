# AGENTS.md

This file defines repository-wide instructions for AI coding agents working on MeetingAI.

## Project Overview

MeetingAI is a cross-platform desktop meeting assistant built with .NET 10 and Avalonia. The active architecture is the modular solution under `src/`:

- `src/MeetingAI.Client/` - WPF application, Views, ViewModels, themes, converters, and composition root.
- `src/MeetingAI.Core/` - business logic, audio recording, transcription, summaries, providers, resilience, repositories, and state.
- `src/MeetingAI.Shared/` - shared configuration, secure storage, hotkeys, logging, constants, and localization.
- `tests/MeetingAI.Core.Tests/` - xUnit tests for core/shared behavior.
- `docs/` - design and setup documentation.

Prefer this modular `src/MeetingAI.Client`, `src/MeetingAI.Core`, and `src/MeetingAI.Shared` structure for new work. The root-level WPF files and `src/MeetingAI/` appear to be legacy or transitional code unless the task explicitly targets them.

## Required Environment

- Windows 10/11 or macOS.
- .NET 10 SDK.
- Visual Studio 2022 / JetBrains Rider, or equivalent .NET tooling.
- Network/API access only when a task explicitly requires real AI provider calls.

## Common Commands

Run commands from the repository root unless a task says otherwise.

```powershell
dotnet restore MeetingAI.sln
dotnet build MeetingAI.sln
dotnet test MeetingAI.sln
dotnet test tests\MeetingAI.Core.Tests\MeetingAI.Core.Tests.csproj
dotnet publish src\MeetingAI.Client\MeetingAI.Client.csproj -c Release -p:Optimize=true
```

Use `dotnet format MeetingAI.sln` when formatting is needed or after broad C# edits. Do not introduce formatting churn in unrelated files.

## Development Workflow

1. Inspect existing patterns before changing code.
2. Keep changes focused on the requested behavior.
3. Prefer small, testable changes over broad rewrites.
4. Add or update tests when changing core logic, providers, configuration, security, parsing, persistence, or state transitions.
5. Run the narrowest useful verification first, then `dotnet build MeetingAI.sln` or `dotnet test MeetingAI.sln` when the change warrants it.
6. Do not revert user changes or clean up unrelated dirty files.

## Coding Standards

- Use C# nullable reference types correctly; avoid suppressing warnings unless there is a documented reason.
- Follow existing naming and folder conventions.
- Prefer dependency injection over service locators or static state.
- Keep UI logic in ViewModels and services where practical; avoid putting business logic in code-behind.
- Use async APIs for I/O and provider calls. Pass `CancellationToken` through public async flows where existing APIs support it.
- Prefer `System.Text.Json` for JSON unless an existing component uses another serializer.
- Keep comments sparse and useful. Explain non-obvious reasoning, not routine assignments.
- Avoid adding new dependencies unless they materially simplify the implementation and fit the project.

## Architecture Guidelines

### Client

- Use MVVM with `CommunityToolkit.Mvvm`.
- Keep Views in `src/MeetingAI.Client/Views/` and ViewModels in `src/MeetingAI.Client/ViewModels/`.
- Place reusable XAML resources in `src/MeetingAI.Client/Themes/`.
- Keep UI responsive; long-running work must not block the UI thread.

### Core

- Keep provider-agnostic meeting logic in services under `src/MeetingAI.Core/Services/`.
- Keep AI provider contracts in `src/MeetingAI.Core/Providers/Abstractions/`.
- Put provider implementations in `src/MeetingAI.Core/Providers/Implementations/`.
- Register or construct new providers through the existing provider factory pattern.
- Retry behavior is handled via Polly in `BaseAIProvider.CreateRetryPolicy()` — preserve it when modifying provider HTTP calls.

### Shared

- Store application settings and secure configuration in `src/MeetingAI.Shared/Configuration/`.
- Use `SecureStorage` for sensitive values such as API keys.
- Keep localization resources in `src/MeetingAI.Shared/i18n/`.
- Keep global hotkey behavior in `src/MeetingAI.Shared/Helpers/GlobalHotkeyService.cs`.

## Testing Guidelines

- Use xUnit for tests.
- Existing test libraries include Moq, NSubstitute, FluentAssertions, and coverlet.
- Put tests beside the relevant domain folder under `tests/MeetingAI.Core.Tests/`.
- Prefer deterministic unit tests. Mock AI providers, network calls, audio devices, clocks, and file-system boundaries unless the test is explicitly integration-oriented.
- Do not commit generated test output such as `TestResults/`, coverage reports, `bin/`, or `obj/`.

## Security And Privacy

- Never hard-code API keys, access tokens, personal data, machine-specific paths, or secrets.
- API keys must be encrypted through AES-256-GCM-backed `SecureStorage`.
- Validate user-controlled file paths, provider configuration, and imported/exported data.
- Avoid logging secrets, full transcripts containing sensitive content, or raw provider credentials.
- When changing provider HTTP calls, preserve timeout, cancellation, and error-handling behavior.

## UI Guidelines

- Follow `docs/UI-Design-System.md` and existing XAML resources.
- Maintain accessible contrast, keyboard navigation, and visible focus states.
- Keep copy concise and consistent across Chinese and English resources.
- Do not introduce decorative UI complexity that conflicts with the existing professional desktop-tool style.

## Git And Files

- This repository may have a dirty worktree. Treat unrelated modifications as user-owned.
- Do not run destructive commands such as `git reset --hard`, `git checkout --`, or recursive deletion unless the user explicitly asks.
- Do not edit generated outputs, build artifacts, or coverage files unless the task explicitly targets them.
- Keep documentation updates in sync with behavior changes when commands, architecture, or user-facing workflows change.

## Completion Checklist

Before finishing a coding task, confirm:

- The implementation matches the active `src/` architecture.
- Relevant tests were added or updated, or the reason for skipping tests is clear.
- `dotnet build MeetingAI.sln` or a narrower relevant command was run when feasible.
- No secrets, generated artifacts, or unrelated user changes were included.
