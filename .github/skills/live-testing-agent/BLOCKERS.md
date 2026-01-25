# Blocker Detection & User Notifications

Patterns for detecting situations requiring human intervention, with notification templates.

## Detection Methods

### 1. Snapshot Analysis
Use `mcp_io_github_chr_take_snapshot` and search for keywords:
```javascript
// Login detection
snapshot.includes("Sign in") || snapshot.includes("Login") || snapshot.includes("Welcome!")

// Docker issues (Aspire dashboard)
snapshot.includes("Runtime unhealthy") || snapshot.includes("Container runtime")

// Error states
snapshot.includes("Failed to fetch") || snapshot.includes("Error") || snapshot.includes("Something went wrong")
```

### 2. Network/Console Errors
Use `mcp_io_github_chr_list_console_messages` to check for:
- `error` type messages
- CORS errors
- Failed fetch requests

### 3. Terminal Output
Check background process output for:
- "ECONNREFUSED"
- "port already in use"
- Build/compile errors

---

## Blocker: Authentication Required

### Detection
Snapshot contains elements like:
- `button "Sign in with Microsoft"`
- `heading "Welcome!"`
- `StaticText "Sign in with your personal Microsoft account"`

### Notification Template
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚠️  USER ACTION REQUIRED: Sign In
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

The application requires authentication to continue testing.

**Please do the following:**
1. Look at the browser window showing the login page
2. Click "Sign in with Microsoft"
3. Complete the authentication (use your Microsoft account)
4. Wait for the redirect to complete

**When done:** Reply with "done" or "signed in"

I'll verify your login and continue the test.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### Verification After Action
1. Take new snapshot
2. Check for user-specific elements (username, avatar, dashboard)
3. Verify login elements are gone

---

## Blocker: Docker Not Running

### Detection
Aspire dashboard snapshot contains:
- `StaticText "Runtime unhealthy"`
- `StaticText "Container runtime 'docker' was found but appears to be unhealthy"`
- Multiple resources showing "Waiting" state

### Notification Template
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚠️  USER ACTION REQUIRED: Start Docker
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

The backend services require Docker to run databases and message queues.

**Current Status:**
- PostgreSQL: ❌ Waiting (needs Docker)
- Redis: ❌ Waiting (needs Docker)
- Service Bus Emulator: ❌ Waiting (needs Docker)

**Please do the following:**
1. Open Docker Desktop
2. Wait for it to fully start (whale icon stops animating)
3. Verify Docker is running: the system tray icon should be stable

**When done:** Reply with "done" or "Docker started"

I'll check the Aspire dashboard and verify services are healthy.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### Verification After Action
1. Wait 10-15 seconds for containers to start
2. Refresh Aspire dashboard
3. Check resource states changed from "Waiting" to "Running"

---

## Blocker: Frontend Server Not Running

### Detection
- `mcp_io_github_chr_new_page` fails with connection error
- Console shows "ERR_CONNECTION_REFUSED"
- Page shows browser error page

### Auto-Resolution Attempt
```powershell
Set-Location "e:\Repos\my\github\mfe-portal\frontend\shell"
npm start
# Run as background process
```

### Notification Template (if auto-start fails)
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚠️  USER ACTION REQUIRED: Start Frontend Server
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

The frontend development server is not running on port 1234.

I attempted to start it automatically but encountered an issue.

**Please run manually:**
```powershell
cd frontend/shell
npm start
```

**Expected output:**
```
> piral debug
ℹ > Running at http://localhost:1234/
✔ Ready!
```

**When done:** Reply with "done" or "server started"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## Blocker: Backend Server Not Running

### Detection
- API calls return "Failed to fetch"
- Aspire dashboard not accessible at https://localhost:15001

### Auto-Resolution Attempt
```powershell
Set-Location "e:\Repos\my\github\mfe-portal\backend\MfePortal.AppHost"
dotnet run
# Run as background process
```

### Notification Template (if auto-start fails)
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚠️  USER ACTION REQUIRED: Start Backend Server
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

The .NET Aspire backend is not running.

**Please run:**
```powershell
cd backend/MfePortal.AppHost
dotnet run
```

**Expected output:**
```
info: Aspire.Hosting.DistributedApplication[0]
      Now listening on: https://localhost:15001
```

**When done:** Reply with "done" or "backend started"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## Blocker: Port Already in Use

### Detection
Terminal output contains:
- "address already in use"
- "EADDRINUSE"
- "port 1234 is already in use"

### Notification Template
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚠️  USER ACTION REQUIRED: Port Conflict
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Port [PORT] is already in use by another process.

**Options:**

**Option 1:** Kill the existing process
```powershell
# Find process using the port
netstat -ano | findstr :[PORT]
# Kill it (replace PID with actual process ID)
taskkill /PID [PID] /F
```

**Option 2:** The server may already be running
- Check if http://localhost:[PORT] is accessible
- If working, reply "already running"

**When done:** Reply with "done" or "already running"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## Blocker: API Configuration Mismatch

### Detection
- Frontend shows "Failed to fetch" 
- Backend is running but on different URL than frontend config expects

### Notification Template
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ℹ️  CONFIGURATION NOTICE: API URL Mismatch
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

The frontend is configured to call a different API than what's running locally.

**Current config:** `frontend/shell/src/config.ts`
```typescript
api: {
  baseUrl: '[CONFIGURED_URL]'
}
```

**Local backend URL:** `http://localhost:[PORT]`

**To test with local backend:**
I can temporarily update the config, or you can update it manually.

**Reply with:**
- "update config" - I'll change it for local testing
- "use deployed" - Continue testing against the deployed API
- "skip" - Skip API-dependent tests
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## Blocker: Build Errors

### Detection
- `npm run build` exits with non-zero code
- `dotnet build` shows errors

### Notification Template
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
❌  BUILD FAILED: Cannot Proceed with Testing
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

The [frontend/backend] build failed with errors:

```
[BUILD ERROR OUTPUT]
```

**This must be fixed before testing can continue.**

Would you like me to:
1. Analyze and fix the build errors
2. Skip testing until you fix them manually

**Reply with:** "fix" or "skip"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## User Response Patterns

Accept these responses as confirmation:
- "done", "Done", "DONE"
- "yes", "ok", "okay", "ready"
- "signed in", "logged in"
- "started", "running"
- "fixed", "resolved"
- "continue", "proceed"

Accept these as skip/abort:
- "skip", "cancel", "abort"
- "later", "not now"
- "stop"
