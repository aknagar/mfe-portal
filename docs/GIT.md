# Git Workflow

This document describes the standard git workflow for this repository, including how to use git worktrees for isolated feature development.

---

## Branching Strategy

All development happens on short-lived branches created from `main`. The `main` branch is the single source of truth and is only updated via GitHub pull requests.

| Branch prefix | Purpose |
|---------------|---------|
| `feature/<description>` | New features |
| `fix/<description>` | Bug fixes |
| `docs/<description>` | Documentation-only changes |
| `chore/<description>` | Maintenance, tooling, config |

---

## Worktree Workflow

This project uses [git worktrees](https://git-scm.com/docs/git-worktree) to isolate each branch in its own directory. This allows multiple branches to be worked on simultaneously without switching branches in the primary workspace.

### Why worktrees?

- Multiple AI agent sessions can work in parallel without interfering with each other or with `main`
- No need to stash or discard local changes when switching context
- The primary workspace stays on `main` and always has a clean state

### Directory convention

All worktrees live under `.worktrees/` in the project root. This directory is gitignored and never committed.

```
.worktrees/
  feature/my-feature/
  fix/some-bug/
  docs/update-readme/
```

---

## Standard Workflow (Step by Step)

### 1. Update local `main`

Always start from a fresh `main` to avoid branching from stale history:

```bash
git pull
```

### 2. Create a worktree

Branch from the now-current local `main`:

```bash
git worktree add .worktrees/<branch-name> -b <branch-name> main
```

Example:

```bash
git worktree add .worktrees/feature/user-auth -b feature/user-auth main
```

### 3. Do work in the worktree

All edits, file writes, and commands must run inside the worktree directory. Use the `workdir` parameter (or `cd`) accordingly.

Commit frequently with descriptive messages following the [Conventional Commits](https://www.conventionalcommits.org/) format:

```
feat(scope): short description
fix(scope): short description
docs(scope): short description
```

### 4. Push the branch to origin

```bash
git push -u origin <branch-name>
```

### 5. Open a Pull Request on GitHub

Create a PR targeting `main`. All merges to `main` go through a PR — never merge locally for code changes.

- CI must pass before merging
- At least one review is recommended for code changes
- Non-code assets (docs, config, slash commands) may be merged with less ceremony

### 6. After the PR is merged on GitHub

Pull the merged commit into local `main`, then clean up:

```bash
# Update local main
git pull

# Delete the local branch
git branch -D <branch-name>

# Remove the worktree
git worktree remove .worktrees/<branch-name>
```

---

## Full Lifecycle at a Glance

```
origin/main
    │
    ▼
git pull                          ← keep local main fresh
    │
    ▼
git worktree add .worktrees/<branch> -b <branch> main
    │
    ▼
  work + commit (inside worktree)
    │
    ▼
git push -u origin <branch>       ← push for PR
    │
    ▼
  PR on GitHub → CI passes → review → merge
    │
    ▼
git pull                          ← pulls merged commit into local main
git branch -D <branch>            ← delete local branch
git worktree remove .worktrees/<branch>   ← clean up worktree
```

---

## Rules and Guardrails

| Rule | Reason |
|------|--------|
| Always `git pull` before creating a worktree | Prevents branching from stale history |
| Never check out `main` in a worktree | `main` is already checked out in the primary workspace; git disallows duplicate checkouts |
| Never merge feature branches into `main` locally | All merges go through GitHub PRs for review and CI |
| One branch per worktree, one concern per branch | Keeps changes focused and reviewable |
| Remove worktree and branch after PR is merged | Prevents stale worktree accumulation |
| Run `git worktree prune` if directories were deleted manually | Cleans up stale worktree metadata git still tracks |

---

## Handling Stale Worktrees

If a worktree directory was deleted manually (e.g. via file explorer), git still tracks it. Clean up with:

```bash
git worktree prune
```

To remove a worktree that has uncommitted changes (e.g. abandoned work that was already merged via PR):

```bash
git worktree remove --force .worktrees/<branch-name>
git branch -D <branch-name>
```

---

## Exception: Local Merge

A local merge (without a PR) is acceptable **only** for non-code assets where review is not required and the PR mechanism is unavailable. This is the exception, not the rule.

```bash
git checkout main
git merge <branch-name>
git branch -d <branch-name>
git worktree remove .worktrees/<branch-name>
```

---

## Reference

- [git-worktree documentation](https://git-scm.com/docs/git-worktree)
- [Conventional Commits](https://www.conventionalcommits.org/)
- Project agent instructions: `AGENTS.md`
- Full project guidelines: `.github/copilot-instructions.md`
