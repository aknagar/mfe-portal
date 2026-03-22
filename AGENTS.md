# Agent Instructions

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for full project guidelines.

## Quick Reference

- **Worktree required**: Always create `.worktrees/<branch-name>` before editing code
- **Build backend**: `dotnet build backend/MfePortal.Backend.sln`
- **Run full stack**: `dotnet run --project backend/MfePortal.AppHost/MfePortal.AppHost.csproj`
- **Build frontend**: `cd frontend/shell && npm run build`
- **Test backend**: `dotnet test backend/MfePortal.Backend.sln`
- **Test frontend**: `cd frontend && npx playwright test`
