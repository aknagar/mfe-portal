# Git Workflow

## Worktree Branching Workflow

### 1. Update local `main`

```bash
git pull
```

### 2. Create a worktree branched from `main`

```bash
git worktree add .worktrees/<branch-name> -b <branch-name> main
```

### 3. Do work inside the worktree

All edits and commands run inside the worktree directory.

### 4. Push and open a Pull Request

```bash
git push -u origin <branch-name>
```

Create a PR on GitHub targeting `main`. All merges to `main` go through a PR.

### 5. After the PR is merged on GitHub

```bash
git pull
git branch -D <branch-name>
git worktree remove .worktrees/<branch-name>
```

---

## AI Agent Workflow

AI agents (OpenCode, Copilot, etc.) follow this workflow automatically via `AGENTS.md`.
This section documents it for reference.

### 1. Session start guard

```bash
git branch --show-current   # must NOT be "main"
```

If on `main`, the agent creates a worktree first (step 2). Never skip this check.

### 2. Create worktree

```bash
git worktree add .worktrees/<branch-name> -b <branch-name> main
```

### 3. Do all work inside the worktree

The agent operates in `.worktrees/<branch-name>` for the entire session.

### 4. Push and open a PR

```bash
git push -u origin <branch-name>
gh pr create --base main --head <branch-name>
```

### 5. After merge (same as human workflow)

```bash
git pull
git branch -D <branch-name>
git worktree remove .worktrees/<branch-name>
```
