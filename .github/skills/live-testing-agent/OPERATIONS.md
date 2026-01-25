# Test Operations

Step-by-step procedures for common testing scenarios.

## Operation: Start Test Session

### Prerequisites Check
```
1. run_in_terminal: Test-Path "frontend/shell/node_modules" 
   → If false: npm install in frontend/shell

2. run_in_terminal: dotnet build backend/MfePortal.Backend.sln
   → Must succeed before continuing
```

### Start Frontend
```
1. run_in_terminal (background):
   Set-Location "frontend/shell"; npm start
   
2. Wait 15 seconds for build

3. mcp_io_github_chr_new_page: http://localhost:1234
   → If connection refused: wait 10s, retry (max 3 times)
```

### Start Backend (Optional - for API testing)
```
1. run_in_terminal (background):
   Set-Location "backend/MfePortal.AppHost"; dotnet run

2. Wait 20 seconds for startup

3. mcp_io_github_chr_new_page: https://localhost:15001
   → Check for Aspire dashboard
   → If Docker blocker detected: notify user
```

---

## Operation: Navigate and Verify Page

### Steps
```
1. mcp_io_github_chr_new_page(url, timeout=30000)

2. mcp_io_github_chr_take_snapshot()
   → Parse for expected elements
   → Check for blocker patterns

3. IF blocker detected:
   → Notify user (see BLOCKERS.md)
   → Wait for confirmation
   → Retry from step 1

4. mcp_io_github_chr_take_screenshot()
   → Save to test-results/[timestamp]/

5. Return snapshot for further actions
```

### Example: Navigate to Approvals
```javascript
// Step 1: Open page
mcp_io_github_chr_new_page({ url: "http://localhost:1234/approvals", timeout: 30000 })

// Step 2: Verify content
snapshot = mcp_io_github_chr_take_snapshot()
expected = ["Approvals", "Pending Approvals", "Refresh"]
missing = expected.filter(e => !snapshot.includes(e))

if (missing.length > 0) {
  // Check if login blocker
  if (snapshot.includes("Sign in")) {
    notifyUser("AUTH_REQUIRED")
  } else {
    reportError("Missing elements: " + missing.join(", "))
  }
}
```

---

## Operation: Click Element

### Steps
```
1. mcp_io_github_chr_take_snapshot()
   → Find target element uid

2. Verify element exists and is clickable
   → Check for "button", "link", or clickable role

3. mcp_io_github_chr_click({ uid: "TARGET_UID" })

4. Wait for response (if needed):
   → mcp_io_github_chr_wait_for({ text: "expected result" })
   OR
   → setTimeout 2000ms then take_snapshot

5. Verify action result
```

### Example: Click Refresh Button
```javascript
// Find the button
snapshot = mcp_io_github_chr_take_snapshot()
// Look for: uid=X_Y button "Refresh"
refreshButton = findUidByText(snapshot, "Refresh", "button")

// Click it
mcp_io_github_chr_click({ uid: refreshButton })

// Wait and verify
await sleep(2000)
newSnapshot = mcp_io_github_chr_take_snapshot()

// Check for loading state or results
if (newSnapshot.includes("Loading")) {
  await mcp_io_github_chr_wait_for({ text: "pending approvals", timeout: 10000 })
}
```

---

## Operation: Fill Form

### Steps
```
1. mcp_io_github_chr_take_snapshot()
   → Find input field uid

2. mcp_io_github_chr_click({ uid: "INPUT_UID" })
   → Focus the input

3. mcp_io_github_chr_fill({ uid: "INPUT_UID", value: "text to enter" })

4. Verify value was entered:
   → take_snapshot and check input value
```

### Example: Fill Multiple Fields
```javascript
// Use fill_form for multiple inputs
mcp_io_github_chr_fill_form({
  elements: [
    { uid: "name_input_uid", value: "Test Order" },
    { uid: "quantity_uid", value: "5" },
    { uid: "cost_uid", value: "1500" }
  ]
})
```

---

## Operation: Test Approvals Page

### Full Flow
```
PHASE 1: Setup
├── Verify frontend running
├── Open http://localhost:1234/approvals
└── Handle login if required

PHASE 2: Verify Page Structure
├── Check heading "Approvals" present
├── Check "Pending Approvals" card present
├── Check "Refresh" button present
├── Screenshot: approvals-page-loaded.png

PHASE 3: Test Refresh Action
├── Click "Refresh" button
├── Wait for response
├── Verify no errors OR expected empty state
├── Screenshot: approvals-after-refresh.png

PHASE 4: Test with Mock Data (if no real data)
├── Use evaluate_script to inject mock approvals
├── Verify approval items render
├── Screenshot: approvals-with-data.png

PHASE 5: Report Results
└── Generate test report with all screenshots
```

### Implementation
```javascript
// Phase 1: Setup
await startFrontendIfNeeded()
page = await mcp_io_github_chr_new_page({ url: "http://localhost:1234/approvals" })
snapshot = await mcp_io_github_chr_take_snapshot()

if (detectLoginRequired(snapshot)) {
  notifyUser("AUTH_REQUIRED")
  await waitForUserConfirmation()
  snapshot = await mcp_io_github_chr_take_snapshot()
}

// Phase 2: Verify Structure
const requiredElements = [
  { text: "Approvals", role: "heading" },
  { text: "Pending Approvals", role: "heading" },
  { text: "Refresh", role: "button" }
]

const results = verifyElements(snapshot, requiredElements)
await mcp_io_github_chr_take_screenshot({ filePath: "test-results/approvals-loaded.png" })

// Phase 3: Test Refresh
const refreshBtn = findElement(snapshot, "Refresh", "button")
await mcp_io_github_chr_click({ uid: refreshBtn })
await sleep(2000)
snapshot = await mcp_io_github_chr_take_snapshot()

const hasError = snapshot.includes("Failed to fetch") || snapshot.includes("Error")
await mcp_io_github_chr_take_screenshot({ filePath: "test-results/approvals-refreshed.png" })

// Phase 5: Report
generateReport({
  name: "Approvals Page Test",
  status: hasError ? "PARTIAL" : "PASS",
  steps: results,
  screenshots: ["approvals-loaded.png", "approvals-refreshed.png"]
})
```

---

## Operation: Test Order Workflow (E2E)

### Prerequisites
- Backend running with healthy containers
- User authenticated

### Flow
```
1. Navigate to order creation page/API
2. Submit order >= $1000 (triggers approval)
3. Navigate to /approvals
4. Verify pending approval appears
5. Click "Approve" button
6. Verify approval processed
7. Check order status updated
```

---

## Operation: Capture Debug Artifacts

### On Test Failure
```
1. mcp_io_github_chr_take_screenshot({ 
     filePath: "test-results/[test-name]-failure.png",
     fullPage: true 
   })

2. mcp_io_github_chr_list_console_messages()
   → Save to test-results/[test-name]-console.txt

3. mcp_io_github_chr_list_network_requests()
   → Filter for errors (4xx, 5xx)
   → Save to test-results/[test-name]-network.txt

4. mcp_io_github_chr_take_snapshot({ verbose: true })
   → Save full a11y tree to test-results/[test-name]-snapshot.txt
```

---

## Operation: Cleanup

### After Test Session
```
1. Close browser pages (keep at least one):
   mcp_io_github_chr_list_pages()
   For each extra page: mcp_io_github_chr_close_page({ pageIdx: N })

2. Stop background processes (optional):
   → Frontend/backend keep running for quick iteration
   → Only terminate if user requests

3. Save test artifacts:
   → Collect all screenshots
   → Generate summary report
```

---

## Helper Functions (Pseudocode)

### detectLoginRequired(snapshot)
```javascript
const loginIndicators = [
  "Sign in with Microsoft",
  "Welcome!",
  "Sign in with your personal Microsoft account"
]
return loginIndicators.some(i => snapshot.includes(i))
```

### findElement(snapshot, text, role)
```javascript
// Parse snapshot lines like: uid=3_25 button "Refresh"
const regex = new RegExp(`uid=(\\d+_\\d+)\\s+${role}.*?"${text}"`)
const match = snapshot.match(regex)
return match ? match[1] : null
```

### verifyElements(snapshot, elements)
```javascript
return elements.map(el => ({
  ...el,
  found: snapshot.includes(el.text),
  status: snapshot.includes(el.text) ? "✅" : "❌"
}))
```

### waitForUserConfirmation()
```javascript
// This happens naturally - Claude waits for user's next message
// User replies "done", "signed in", etc.
// Then continue with next steps
```
