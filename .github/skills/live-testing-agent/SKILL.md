---
name: live-testing-agent
description: Perform live browser testing using MCP browser tools. Use when user asks to test UI, verify features, or run E2E tests. Automatically detects blockers requiring user intervention (login, Docker, server startup) and provides clear notifications.
---

# Live Testing Agent

Automated browser testing using Chrome DevTools MCP tools. Detects human-intervention blockers and notifies user with clear action items.

## When to Use

- User asks: "test the approvals page", "verify the login works", "check if the feature is working"
- After implementing UI changes to validate they render correctly
- Running smoke tests or E2E flows

## Core Browser Tools

| Tool | Purpose |
|------|---------|
| `mcp_io_github_chr_new_page` | Open URL in browser |
| `mcp_io_github_chr_take_snapshot` | Get page accessibility tree (preferred for element detection) |
| `mcp_io_github_chr_take_screenshot` | Capture visual state |
| `mcp_io_github_chr_click` | Click element by uid |
| `mcp_io_github_chr_fill` | Type into input field |
| `mcp_io_github_chr_evaluate_script` | Run JavaScript on page |
| `mcp_io_github_chr_wait_for` | Wait for text to appear |

## Test Workflow

```
1. START SERVICES
   ├── Check if frontend running (port 1234)
   ├── Check if backend running (Aspire dashboard)
   └── If not → start them as background processes

2. NAVIGATE TO PAGE
   ├── Open target URL with mcp_io_github_chr_new_page
   └── Take snapshot to verify page loaded

3. DETECT BLOCKERS (see BLOCKERS.md)
   ├── Login required? → Notify user
   ├── Docker unhealthy? → Notify user  
   ├── API errors? → Check backend status
   └── If blocker → WAIT for user, then retry

4. EXECUTE TEST ACTIONS
   ├── Find elements via snapshot uid
   ├── Perform clicks, fills, navigations
   └── Capture screenshots at key points

5. VERIFY RESULTS
   ├── Check expected elements present
   ├── Verify no error states
   └── Report pass/fail with evidence
```

## Blocker Detection Patterns

### Login Required
**Snapshot contains:** "Sign in", "Login", "Welcome!" with sign-in button
**Action:** Notify user to sign in manually

```
⚠️ USER ACTION REQUIRED: Authentication

The page requires login. Please:
1. Click "Sign in with Microsoft" in the browser
2. Complete the authentication flow
3. Reply "done" when you're signed in

I'll continue testing after you confirm.
```

### Docker Not Running
**Snapshot contains:** "Runtime unhealthy", "Container runtime"
**Action:** Notify user to start Docker

```
⚠️ USER ACTION REQUIRED: Start Docker

The backend requires Docker for databases and services.
Please:
1. Start Docker Desktop
2. Wait for it to be ready (whale icon stops animating)
3. Reply "done" when Docker is running

I'll verify the services and continue.
```

### Server Not Running
**Error:** Connection refused, fetch failed, ECONNREFUSED
**Action:** Start the server or notify user

```
⚠️ SERVICE NOT RUNNING: [service name]

The [frontend/backend] server is not running.
Starting it now... (or: Please start it manually)

[If auto-start fails]
Please run:
  cd [path]
  [command]
Then reply "done".
```

## Service Startup Commands

### Frontend (port 1234)
```powershell
Set-Location "frontend/shell"
npm start
# Background: isBackground=true, timeout=0
```

### Backend (Aspire)
```powershell
Set-Location "backend/MfePortal.AppHost"
dotnet run
# Background: isBackground=true, timeout=0
```

## Test Report Format

After completing a test, report:

```
## Test Results: [Test Name]

**Status:** ✅ PASS | ❌ FAIL | ⚠️ PARTIAL

**Steps Executed:**
1. [Step] - ✅
2. [Step] - ✅
3. [Step] - ❌ [reason]

**Screenshots:** [Captured N screenshots]

**Issues Found:**
- [Issue description if any]

**Recommendations:**
- [Next steps if needed]
```

## Example: Test Approvals Page

```
User: "test the approvals page"

1. Check frontend running → npm start if needed
2. Open http://localhost:1234/approvals
3. Take snapshot
4. IF login screen → notify user, wait for "done"
5. Take snapshot again after login
6. Verify elements:
   - Heading "Approvals" present
   - "Pending Approvals" card visible
   - Refresh button clickable
7. Click Refresh button
8. Check for errors or success state
9. Screenshot final state
10. Report results
```

## Polling After User Action

After notifying user of a blocker:
1. Wait for user to reply "done" or similar confirmation
2. Retry the blocked action
3. If still blocked, provide updated guidance
4. Maximum 3 retry attempts before escalating

## See Also

- [BLOCKERS.md](BLOCKERS.md) - Detailed blocker patterns and notifications
- [OPERATIONS.md](OPERATIONS.md) - Step-by-step test operations
