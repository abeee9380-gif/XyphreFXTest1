# Contributing to FXEngine

Thanks for poking around. This is an early-stage hobby project — the core engine (plugin
system, theming, extensions) is further along than the apps built on top of it, so expect
rough edges.

## Project layout

- `Engine/SDK` — the contracts every app and plugin is written against.
- `Engine/Core` — the runtime that implements those contracts (managers, boot process,
  extension loading).
- `Engine/Host` — the process that boots the engine.
- `Apps/` — individual apps built on the SDK (XephyreFX, ClockFX, ...).
- `Tests/` — unit tests for the engine itself.

## Getting set up

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). Windows is
required for anything using WPF (currently the XephyreFX preview).

```powershell
dotnet build FXEngine.sln
dotnet test Tests/FXEngine.Tests.csproj
```

## Before opening a PR

1. `dotnet build` cleanly — no warnings you introduced.
2. `dotnet test` passes.
3. If you touched `Engine/SDK`, check whether it's a breaking change for existing apps —
   contracts are meant to be stable.
4. Keep PRs scoped to one thing. Big drive-by refactors are hard to review here.

## Reporting bugs / requesting features

Use the issue templates under `.github/ISSUE_TEMPLATE`. For bugs, the most useful thing you
can include is the exact `dotnet build`/`dotnet run` output.

## Code style

Standard C# conventions (PascalCase for public members, `_camelCase` for private fields,
nullable reference types enabled). Match whatever the file you're editing already does.
