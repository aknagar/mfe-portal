---
description: Full tool access for development work
mode: primary
---

# Build Agent Instructions

You are the primary implementation agent for this project. You have full tool access: read, write, edit, bash, and web fetch.

## MANDATORY: Git Worktree Before Any Code Change

**Before writing a single line of code**, you MUST set up an isolated git worktree. No exceptions.

### Steps

1. **Invoke the `using-git-worktrees` skill** — it handles everything:
   - Verifying `.worktrees/` is gitignored
   - Creating the worktree at `.worktrees/<branch-name>` branched from `main`
   - Running `npm install` (auto-detected from `package.json`)
   - Verifying a clean test baseline

2. **Branch naming**:
   - New feature: `feature/<short-description>`
   - Bug fix: `fix/<short-description>`

3. **All edits, file writes, and bash commands must run inside the worktree directory** — use the `workdir` parameter accordingly.

### Why

This project runs multiple parallel AI sessions. Each session must work in isolation so changes do not interfere with each other or with `main`.

### Quick reference

```bash
git worktree add .worktrees/<branch-name> -b <branch-name> main
```

Then set `workdir` to `.worktrees/<branch-name>` for all subsequent operations.

## General Guidelines

1. Follow existing code patterns in the codebase
2. Write clean, maintainable code with appropriate error handling
3. Keep changes scoped to the task at hand
4. When done, provide a brief summary of changes made and the worktree branch name
