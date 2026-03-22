# Fix CD Pipeline: Infrastructure Provision - Test

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the `infra-provision-test.yml` GitHub Actions workflow so `aspire deploy` executes successfully without credential failures or CLI errors.

**Architecture:** Four targeted edits to `infra-provision-test.yml` — add missing `environment: test` job gate (OIDC fix), strip non-existent CLI flags from `aspire deploy`, switch all steps from `shell: pwsh` to default bash, and pin the Aspire CLI version to match the AppHost SDK.

**Tech Stack:** GitHub Actions, .NET 10, Aspire CLI 13.1.2, Azure OIDC (Workload Identity Federation)

---

## File Map

| File | Change |
|---|---|
| `.github/workflows/infra-provision-test.yml` | Add `environment: test`, fix shell, fix deploy command, pin CLI version |

No other files are touched.

---

### Task 1: Add `environment: test` to the provision job

**Why:** Azure AD federated credentials are registered with subject claim `repo:<org>/<repo>:environment:test`. Without `environment: test` on the job, GitHub issues a JWT with subject `repo:<org>/<repo>:ref:refs/heads/main`, which does not match — causing `azure/login@v2` to receive a 401 from AAD.

**Evidence:** `infra-provision-prod.yml` line 14 already has `environment: production`. This workflow is the only one missing its environment gate.

**Files:**
- Modify: `.github/workflows/infra-provision-test.yml` — add `environment: test` under the `provision:` job key

- [ ] **Step 1: Open the file and locate the job definition**

  File: `.github/workflows/infra-provision-test.yml`
  
  Find lines 11–13:
  ```yaml
  jobs:
    provision:
      runs-on: ubuntu-latest
  ```

- [ ] **Step 2: Add `environment: test` immediately after `runs-on`**

  Result should look like:
  ```yaml
  jobs:
    provision:
      runs-on: ubuntu-latest
      environment: test
      timeout-minutes: 100
  ```

- [ ] **Step 3: Verify the change looks correct**

  Confirm `environment: test` is at the same indentation level as `runs-on` and `timeout-minutes`.

---

### Task 2: Fix `Install Aspire workload` step — remove `shell: pwsh`

**Why:** All sibling workflows (`cd-backend-deploy-test.yml`, `cd-backend-deploy-prod.yml`) use default bash on `ubuntu-latest`. Using `shell: pwsh` is inconsistent and risks `$PATH` propagation differences between PowerShell and bash sessions on Linux.

**Files:**
- Modify: `.github/workflows/infra-provision-test.yml` — remove `shell: pwsh` from the `Install Aspire workload` step

- [ ] **Step 1: Locate the `Install Aspire workload` step**

  Find lines 31–34:
  ```yaml
  - name: Install Aspire workload
    timeout-minutes: 10
    run: dotnet workload install aspire
    shell: pwsh
  ```

- [ ] **Step 2: Remove the `shell: pwsh` line**

  Result:
  ```yaml
  - name: Install Aspire workload
    timeout-minutes: 10
    run: dotnet workload install aspire
  ```

---

### Task 3: Fix `Install Aspire CLI` step — pin version and remove `shell: pwsh`

**Why:** `--prerelease` allows the CLI to float to any daily build or RC, while the AppHost SDK is pinned to `13.1.2` (`backend/MfePortal.AppHost/MfePortal.AppHost.csproj` line 1: `Aspire.AppHost.Sdk/13.1.2`). Version mismatch between CLI and SDK can cause protocol errors. Remove `shell: pwsh` for consistency (same reason as Task 2).

**Files:**
- Modify: `.github/workflows/infra-provision-test.yml` — update `Install Aspire CLI` step

- [ ] **Step 1: Locate the `Install Aspire CLI` step**

  Find lines 36–39:
  ```yaml
  - name: Install Aspire CLI
    timeout-minutes: 5
    run: dotnet tool install -g Aspire.Cli --prerelease
    shell: pwsh
  ```

- [ ] **Step 2: Replace `--prerelease` with `--version 13.1.2` and remove `shell: pwsh`**

  Result:
  ```yaml
  - name: Install Aspire CLI
    timeout-minutes: 5
    run: dotnet tool install -g Aspire.Cli --version 13.1.2
  ```

---

### Task 4: Fix `Deploy with Aspire` step — strip unsupported flags and remove `shell: pwsh`

**Why:** The current command is:
```
aspire deploy --environment $env:AZURE_ENV_NAME --log-level debug --include-exception-details
```
None of these flags exist on `aspire deploy` in CLI version 13.x. The command fails immediately with "unrecognized option" before doing any deployment work. The CLI reads `AZURE_ENV_NAME` from the job-level `env:` block automatically — no flag needed. The `$env:AZURE_ENV_NAME` syntax is PowerShell-only and would be a literal string under bash anyway.

**Files:**
- Modify: `.github/workflows/infra-provision-test.yml` — simplify the `Deploy with Aspire` step

- [ ] **Step 1: Locate the `Deploy with Aspire` step**

  Find lines 51–61:
  ```yaml
  - name: Deploy with Aspire
    timeout-minutes: 90
    working-directory: ./backend/MfePortal.AppHost
    run: aspire deploy --environment $env:AZURE_ENV_NAME --log-level debug --include-exception-details
    shell: pwsh
    env:
      # Note: Colon (:) is not supported in environment variable names
      # Use double underscore (__) instead. For example: Azure__SubscriptionId
      Azure__SubscriptionId: ${{ env.AZURE_SUBSCRIPTION_ID }}
      Azure__ResourceGroup: ${{ env.AZURE_RESOURCE_GROUP }}
      Azure__Location: ${{ env.AZURE_LOCATION }}
  ```

- [ ] **Step 2: Replace the `run:` line and remove `shell: pwsh`**

  The `env:` block with `Azure__*` variables is harmless — keep it in place to avoid unintended changes. Only modify `run:` and remove `shell: pwsh`.

  Result:
  ```yaml
  - name: Deploy with Aspire
    timeout-minutes: 90
    working-directory: ./backend/MfePortal.AppHost
    run: aspire deploy
    env:
      # Note: Colon (:) is not supported in environment variable names
      # Use double underscore (__) instead. For example: Azure__SubscriptionId
      Azure__SubscriptionId: ${{ env.AZURE_SUBSCRIPTION_ID }}
      Azure__ResourceGroup: ${{ env.AZURE_RESOURCE_GROUP }}
      Azure__Location: ${{ env.AZURE_LOCATION }}
  ```

---

### Task 5: Final file verification

- [ ] **Step 1: Read the complete modified file**

  Read `.github/workflows/infra-provision-test.yml` in full and confirm:
  - Line 13 (or nearby): `environment: test` is present under the `provision:` job
  - `Install Aspire workload` step has no `shell:` key
  - `Install Aspire CLI` step uses `--version 13.1.2` and has no `shell:` key
  - `Deploy with Aspire` step `run:` is just `aspire deploy` with no flags and no `shell:` key
  - No other lines were accidentally changed

- [ ] **Step 2: Confirm the file is valid YAML**

  Run: `python3 -c "import yaml, sys; yaml.safe_load(open('.github/workflows/infra-provision-test.yml'))" && echo "YAML valid"` from the repo root.
  
  Expected: `YAML valid`

---

### Task 6: Commit

- [ ] **Step 1: Stage only the changed file**

  ```bash
  git add .github/workflows/infra-provision-test.yml
  ```

- [ ] **Step 2: Commit with a descriptive message**

  ```bash
  git commit -m "fix: resolve OIDC auth and CLI flag failures in infra-provision-test pipeline

  - Add environment: test to job so OIDC JWT sub claim matches Azure federated credential
  - Strip unsupported --environment, --log-level, --include-exception-details flags from aspire deploy
  - Pin Aspire CLI to 13.1.2 to match AppHost SDK version (was floating --prerelease)
  - Remove shell: pwsh from all steps (Linux runner defaults to bash; matches sibling workflows)"
  ```

- [ ] **Step 3: Verify commit succeeded**

  Run: `git log --oneline -1`
  
  Expected: shows your commit message as HEAD.

---

## Expected Final State of `.github/workflows/infra-provision-test.yml`

```yaml
name: Infrastructure Provision - Test

on:
  workflow_dispatch:

permissions:
  id-token: write
  contents: read

jobs:
  provision:
    runs-on: ubuntu-latest
    environment: test
    timeout-minutes: 100
    env:
      AZURE_ENV_NAME: test
      AZURE_CLIENT_ID: ${{ vars.AZURE_CLIENT_ID }}
      AZURE_TENANT_ID: ${{ vars.AZURE_TENANT_ID }}
      AZURE_SUBSCRIPTION_ID: ${{ vars.AZURE_SUBSCRIPTION_ID }}
      AZURE_LOCATION: ${{ vars.AZURE_LOCATION }}      
      AZURE_RESOURCE_GROUP: ${{ vars.AZURE_RESOURCE_GROUP }}
      AZURE_CONTAINER_REGISTRY_ENDPOINT: ${{ vars.AZURE_CONTAINER_REGISTRY_ENDPOINT }}
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install Aspire workload
        timeout-minutes: 10
        run: dotnet workload install aspire

      - name: Install Aspire CLI
        timeout-minutes: 5
        run: dotnet tool install -g Aspire.Cli --version 13.1.2

      - name: Install Dapr CLI
        uses: dapr/setup-dapr@v2      

      - name: Log in to Azure
        uses: azure/login@v2
        with:
          client-id: ${{ env.AZURE_CLIENT_ID }}
          tenant-id: ${{ env.AZURE_TENANT_ID }}
          subscription-id: ${{ env.AZURE_SUBSCRIPTION_ID }}

      - name: Deploy with Aspire
        timeout-minutes: 90
        working-directory: ./backend/MfePortal.AppHost
        run: aspire deploy
        env:
          # Note: Colon (:) is not supported in environment variable names
          # Use double underscore (__) instead. For example: Azure__SubscriptionId
          Azure__SubscriptionId: ${{ env.AZURE_SUBSCRIPTION_ID }}
          Azure__ResourceGroup: ${{ env.AZURE_RESOURCE_GROUP }}
          Azure__Location: ${{ env.AZURE_LOCATION }}
```

## Out of Scope

- `infra-provision-prod.yml` — has the same shell/flag issues but already has `environment: production` and is not the failing workflow
- `cd-backend-deploy-test.yml` / `cd-backend-deploy-prod.yml` — URL extraction stub (separate concern)
- Dapr CLI step (may be unnecessary but removing it is a medium-priority cleanup, not part of this fix)
- `AZURE_CONTAINER_REGISTRY_ENDPOINT` unused variable (cosmetic, not causing failures)
