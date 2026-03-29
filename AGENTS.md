# Agent Instructions

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for full project guidelines.

## Mandatory Session Start — Git Workflow

**BEFORE touching any file, writing any code, or running any command that modifies state:**

1. **Invoke the `using-git-worktrees` skill.**
2. **Guard check — run:**
   ```bash
   git branch --show-current
   ```
   If the output is `main` — **STOP**. Do not proceed until you have completed step 3.
3. **Create a worktree for this task:**
   ```bash
   git worktree add .worktrees/<branch-name> -b <branch-name> main
   ```
   Branch naming: `feature/<desc>` for features, `fix/<desc>` for bug fixes.
4. **Make all edits inside `.worktrees/<branch-name>`** — never commit to the repo root
   while it is checked out to `main`.

## Mandatory Session End — Handoff

After all changes are committed inside the worktree:

1. **Report the branch name** to the user.
2. **Do NOT push** and **do NOT open a PR** — the user will do this manually.

## Quick Reference

- **Build backend**: `dotnet build backend/MfePortal.Backend.sln`
- **Run full stack**: `dotnet run --project backend/MfePortal.AppHost/MfePortal.AppHost.csproj`
- **Build frontend**: `cd frontend/shell && npm run build`
- **Test backend**: `dotnet test backend/MfePortal.Backend.sln`
- **Test frontend**: `cd frontend && npx playwright test`
