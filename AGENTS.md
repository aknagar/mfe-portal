# Project Agent Instructions

## Git Worktree Policy

**MANDATORY: Before making ANY code changes (features, bug fixes, refactors, experiments):**

1. Create a new git worktree branched from `main`
2. Place it at `.worktrees/<branch-name>` (relative to repo root)
3. Branch naming convention:
   - New features: `feature/<short-description>` (e.g., `feature/user-auth`)
   - Bug fixes: `fix/<short-description>` (e.g., `fix/login-redirect`)
4. All changes must be made inside the worktree — never directly on `main`

This ensures each AI session is fully isolated. Changes from one session cannot interfere with another.

### Worktree setup commands

```bash
git worktree add .worktrees/<branch-name> -b <branch-name> main
cd .worktrees/<branch-name>
```

### Why this matters

- Multiple AI sessions can run in parallel without conflicting
- Work is always on a dedicated branch, never polluting `main`
- Easy to review, merge, or discard work per-session

### Worktree directory

`.worktrees/` is gitignored — worktree contents are never accidentally committed to the repository.
