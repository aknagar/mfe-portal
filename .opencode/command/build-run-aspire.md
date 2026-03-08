---
description: Build and run the Aspire AppHost, then open the dashboard URL in the browser
---

Build and run the .NET Aspire AppHost for this project and open the dashboard in the browser for visual inspection.

**Steps**

1. **Check if Aspire is already running**

   Run:
   ```bash
   netstat -ano | findstr "15001"
   ```

   - If a port is listening, skip to step 3 to read the log for the login URL.
   - If nothing is listening, proceed to step 2.

2. **Start the Aspire AppHost in a new persistent window**

   Clear any previous log and launch:
   ```bash
   powershell -Command "Remove-Item -ErrorAction Ignore 'E:\\Repos\\my\\github\\mfe-portal\\apphost-run.log'; Start-Process powershell -ArgumentList '-NoExit', '-Command', 'cd E:\\Repos\\my\\github\\mfe-portal\\backend; dotnet run --project MfePortal.AppHost/MfePortal.AppHost.csproj --launch-profile https 2>&1 | Tee-Object -FilePath E:\\Repos\\my\\github\\mfe-portal\\apphost-run.log'"
   ```

   Then poll the log file until the login URL appears (check every 10 seconds, up to 2 minutes):
   ```bash
   powershell -Command "
     $timeout = 120; $elapsed = 0; $url = $null
     while ($elapsed -lt $timeout) {
       Start-Sleep 10; $elapsed += 10
       if (Test-Path 'E:\\Repos\\my\\github\\mfe-portal\\apphost-run.log') {
         $content = Get-Content 'E:\\Repos\\my\\github\\mfe-portal\\apphost-run.log' -Raw
         if ($content -match 'Login to the dashboard at (https://\S+)') {
           $url = $matches[1]; break
         }
       }
     }
     if ($url) { Write-Output $url } else { Write-Output 'TIMEOUT' }
   "
   ```

3. **Extract and open the dashboard URL**

   Parse the login URL from the log:
   ```bash
   powershell -Command "Get-Content 'E:\\Repos\\my\\github\\mfe-portal\\apphost-run.log' | Select-String 'Login to the dashboard at' | Select-Object -Last 1"
   ```

   Then open it in the default browser:
   ```bash
   powershell -Command "Start-Process '<url>'"
   ```
   Replace `<url>` with the actual URL extracted above.

4. **Report to the user**

   Output the URL so the user can also navigate to it manually:

   ```
   Aspire dashboard is running.

   URL: https://localhost:15001/login?t=<token>

   The dashboard has been opened in your browser.
   Keep the Aspire terminal window open to maintain the session.
   ```

**Notes**
- The login token changes every time the AppHost restarts.
- The AppHost window must stay open for the dashboard to remain accessible.
- If the build takes longer than 2 minutes, re-run the command.
