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

## Mandatory Session End — PR Workflow

After all changes are committed inside the worktree:

1. **Push the branch:**
   ```bash
   git push -u origin <branch-name>
   ```
2. **Open a PR targeting `main`:**
   ```bash
   gh pr create --base main --head <branch-name> --title "<title>" --body "<summary>"
   ```
3. **Report the PR URL** to the user before ending the session.

## Quick Reference

- **Build backend**: `dotnet build backend/MfePortal.Backend.sln`
- **Run full stack**: `dotnet run --project backend/MfePortal.AppHost/MfePortal.AppHost.csproj`
- **Build frontend**: `cd frontend/shell && npm run build`
- **Test backend**: `dotnet test backend/MfePortal.Backend.sln`
- **Test frontend**: `cd frontend && npx playwright test`
