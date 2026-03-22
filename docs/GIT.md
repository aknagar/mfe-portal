# Git Workflow

## Branching Strategy

All development happens on short-lived branches created from `main`. The `main` branch is the single source of truth and is only updated via GitHub pull requests.

| Branch prefix | Purpose |
|---------------|---------|
| `feature/<description>` | New features |
| `fix/<description>` | Bug fixes |
| `docs/<description>` | Documentation-only changes |
| `chore/<description>` | Maintenance, tooling, config |
