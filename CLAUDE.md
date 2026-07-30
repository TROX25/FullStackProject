# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commit behavior

When the user types "commit":
1. Stage all changed files
2. Generate a commit message following the format in `.github/copilot-instructions.md`: `<type>: <short description>` — one line, no body
3. Commit and push to the current branch on both `origin` (Bitbucket) and `github` (GitHub)

## Project overview

AI-Powered Automotive Listings Search — see `README.md` for setup/run, `ARCHITECTURE.md` for design, `ASSUMPTIONS.md` for known limitations. Backend: .NET 10 (`MojProjekt.Api`), Clean Architecture. Frontend: Angular 21 standalone components (`mojprojekt-client`).

## Layering rules (Clean Architecture)

- `MojProjekt.Domain` — entities/enums/value objects only. No dependencies on any other project or external package.
- `MojProjekt.Application` — CQRS commands/queries/handlers (MediatR), DTOs, and interfaces implemented elsewhere. Depends only on `Domain`. Never reference EF Core, AngleSharp, ASP.NET Core, or the Anthropic client from here.
- `MojProjekt.Infrastructure` — implements `Application`'s interfaces: EF Core/SQLite persistence, the OLX crawler, the Anthropic query interpreter, the ranking service. Depends on `Application` (+ `Domain` transitively).
- `MojProjekt.Api` — composition root only: DI registration, endpoint mapping, startup migration. Depends on `Application` + `Infrastructure`.

When adding a feature, put the interface + DTOs + handler in `Application`, the concrete implementation in `Infrastructure`, and wire DI registration in `MojProjekt.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs`.

## Adding a new project

New `.csproj` files must be registered in `FullStackProject.slnx` — it's the new XML solution format, not a legacy `.sln`:

```xml
<Solution>
  <Project Path="MojProjekt.Api/MojProjekt.Api.csproj" />
  ...
  <Project Path="YourNewProject/YourNewProject.csproj" />
</Solution>
```

## Build/test/migration commands

```bash
dotnet build                                                    # whole solution
dotnet test                                                     # all .NET test projects
dotnet ef migrations add <Name> \
  --project MojProjekt.Infrastructure \
  --startup-project MojProjekt.Infrastructure                   # not MojProjekt.Api — see AppDbContextFactory
cd mojprojekt-client && npm test && npm run build
```

