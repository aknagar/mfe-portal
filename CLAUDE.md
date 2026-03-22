# Claude Agent Instructions

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for full project guidelines (architecture, build commands, conventions).

## Git Worktree Policy

**MANDATORY**: Create `.worktrees/<branch-name>` before any code changes. See copilot-instructions.md for details.

## Preferred Skills

Use the `using-git-worktrees` skill (available via superpowers plugin) when setting up worktrees. It handles:
- Directory verification
- `.gitignore` safety checks
- Dependency installation
- Baseline test verification
