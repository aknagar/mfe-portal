# OIDC vs Managed Identity

## Quick Comparison
| | OIDC | Managed Identity |
|---|------|-----------------|
| **Use** | External CI/CD (GitHub Actions) | Azure resources (VMs, Apps) |
| **Credentials** | None (ephemeral tokens) | None (auto-rotated) |
| **Token Lifetime** | 5-15 minutes | ~24 hours |
| **Refresh** | Fresh token per workflow run | Automatic by Azure |
| **Rotation** | Not needed | Not needed |
| **Why we chose OIDC** | Purpose-built for GitHub Actions, no secrets to store, short-lived tokens, industry standard |

## Why OIDC for GitHub Actions
- ✅ No stored credentials → no exposure risk
- ✅ Fresh token per workflow run (5-15 min lifetime)
- ✅ Token bound to repo/branch/workflow (prevents reuse)
- ✅ Industry standard across all cloud providers
- ❌ Managed Identity only works for code running *inside* Azure
