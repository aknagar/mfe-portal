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
