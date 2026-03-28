# Project Guidelines

## Git Worktree Policy

**MANDATORY FIRST STEP of every session — before any file edit, scaffold, or command:**

1. **Invoke the `using-git-worktrees` skill.**
2. **Guard check** — run `git branch --show-current`.
   If the result is `main`, halt immediately and complete step 3 before doing anything else.
3. **Create the worktree:**
   ```bash
   git worktree add .worktrees/<branch-name> -b <branch-name> main
   ```
   - Features → `feature/<short-desc>`
   - Bug fixes → `fix/<short-desc>`
4. **Work exclusively inside `.worktrees/<branch-name>`** — never commit to the repo root
   while it points to `main`.
5. **Session end:** push the branch and open a PR:
   ```bash
   git push -u origin <branch-name>
   gh pr create --base main --head <branch-name>
   ```

`.worktrees/` is gitignored.

## Architecture

**Monorepo** with three top-level areas:

| Area | Path | Stack |
|------|------|-------|
| Backend | `backend/` | .NET 10, ASP.NET Core, EF Core, Dapr, Aspire |
| Frontend | `frontend/` | React 18, Piral (microfrontends), Vite, Tailwind, shadcn/UI |
| Feature Specs | `specs/` | Markdown-based feature specifications |

**Backend — Clean Architecture** (dependencies flow inward):
- `AugmentService.Core` — Domain entities, interfaces, zero external deps
- `AugmentService.Application` — MediatR handlers, validators, DTOs
- `AugmentService.Infrastructure` — EF Core repos, external services, DI setup
- `AugmentService.Api` — Controllers, minimal APIs, middleware

**Frontend — Piral Shell/Pilet model**:
- `frontend/shell/` — Admin portal shell (hosts pilets, defines layout/routing)
- `frontend/pilets/` — Independent micro-frontend modules

**Orchestration**: .NET Aspire AppHost (`backend/MfePortal.AppHost/Program.cs`) manages all services, databases (PostgreSQL), messaging (Azure Service Bus + Dapr), and observability.

For architecture details see [backend/docs/ARCHITECTURE.md](../backend/docs/ARCHITECTURE.md) and [docs/FRONTEND.md](../docs/FRONTEND.md).

## Build and Test

### Backend

```bash
# Build
dotnet build backend/MfePortal.Backend.sln

# Run (Aspire orchestrator — starts all services)
dotnet run --project backend/MfePortal.AppHost/MfePortal.AppHost.csproj

# Unit tests
dotnet test backend/MfePortal.Backend.sln

# Single test project
dotnet test backend/tests/AugmentService/AugmentService.Api.UnitTests/
```

### Frontend

```bash
# Install dependencies
cd frontend/shell && npm install

# Dev server (http://localhost:1234)
cd frontend/shell && npm start

# Build
cd frontend/shell && npm run build

# E2E tests (requires frontend running)
cd frontend && npx playwright test
```

### Full Stack

Use VS Code tasks: **"build all"**, **"test all"**, **"aspire run"** (defined in `.vscode/tasks.json`).

## Conventions

### Backend

- **DI registration**: Each layer has `DependencyInjection.cs` with extension methods on `IHostApplicationBuilder` (e.g., `builder.AddApplication()`, `builder.AddInfrastructure()`)
- **Entity creation**: Factory methods returning `FluentResults<T>` for domain validation
- **CQRS**: MediatR with `LoggingBehavior<,>` pipeline
- **Naming**: Entities are singular (`Forecast`, `Order`), interfaces use `I` prefix (`IWeatherRepository`), services suffix with purpose (`ProxyApplicationService`)
- **Package versions**: Centrally managed in `backend/Directory.Packages.props`
- **HTTPS only**: All services require HTTPS (see [backend/docs/PREFERENCES.md](../backend/docs/PREFERENCES.md))
- **Test coverage**: 80% target, Cobertura format (see `backend/coverlet.runsettings`)

### Frontend

- **Components**: PascalCase filenames, shadcn/UI primitives for all UI elements
- **Auth**: MSAL for Azure AD — all protected routes require authentication
- **Styling**: Tailwind CSS utility classes — avoid custom CSS
- **Pilet sharing**: Components exported via `portal-shell/*` namespace
- **Routing**: React Router v5 for shell routes

### General

- **Secrets**: Never commit credentials. Use `.env.local` (frontend), `dotnet user-secrets` (backend), Azure Key Vault (production). See [docs/SECURITY-BEST-PRACTICES.md](../docs/SECURITY-BEST-PRACTICES.md).

## Documentation Index

| Topic | File |
|-------|------|
| Backend architecture | [backend/docs/ARCHITECTURE.md](../backend/docs/ARCHITECTURE.md) |
| Clean architecture layers | [backend/docs/CLEAN_ARCHITECTURE.md](../backend/docs/CLEAN_ARCHITECTURE.md) |
| Frontend & Piral setup | [docs/FRONTEND.md](../docs/FRONTEND.md) |
| Authentication (MSAL) | [frontend/docs/AUTHENTICATION.md](../frontend/docs/AUTHENTICATION.md) |
| Frontend Docker/Aspire | [frontend/docs/SETUP.md](../frontend/docs/SETUP.md) |
| Debugging | [frontend/docs/DEBUG.md](../frontend/docs/DEBUG.md) |
| Dapr integration | [backend/docs/DAPR_SETUP.md](../backend/docs/DAPR_SETUP.md) |
| Aspire configuration | [backend/docs/Aspire-Configuration.md](../backend/docs/Aspire-Configuration.md) |
| Security practices | [docs/SECURITY-BEST-PRACTICES.md](../docs/SECURITY-BEST-PRACTICES.md) |
| Observability | [docs/OBSERVABILITY.md](../docs/OBSERVABILITY.md) |
| Local testing | [backend/docs/TESTING.md](../backend/docs/TESTING.md) |
| API reference | [backend/docs/API_LIST.md](../backend/docs/API_LIST.md) |
| Load testing | [backend/docs/LOAD_TESTING.md](../backend/docs/LOAD_TESTING.md) |
