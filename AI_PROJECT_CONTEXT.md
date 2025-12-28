# AI Project Context - Micro-Frontend Admin Portal

**Last Updated**: December 26, 2025  
**Project Status**: Shell fully functional, pilet loading blocked by framework compatibility

---

## Table of Contents
1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Tech Stack](#tech-stack)
4. [Project Structure](#project-structure)
5. [Development Workflow](#development-workflow)
6. [Current Status](#current-status)
7. [Known Issues](#known-issues)
8. [Key Configuration Files](#key-configuration-files)
9. [MCP Servers](#mcp-servers)
10. [Next Steps](#next-steps)

---

## Project Overview

This is an **Nx monorepo** implementing a **Piral-based micro-frontend architecture** for an admin portal. The project consists of:

- **Shell Application** (admin-portal-shell): Main container that hosts pilets
- **Pilets**: Independently deployable micro-frontends (currently: http-tester-pilet)

### Purpose
Create a modular admin portal where features can be developed, deployed, and maintained independently as pilets (micro-frontends).

### Key Features
- ✅ Collapsible sidebar navigation with icon mode
- ✅ Dashboard, Users, and Settings pages
- ✅ Dark mode support with shadcn/ui
- ✅ Nx workspace for monorepo management
- ⏸️ Dynamic pilet loading (blocked - see Known Issues)

---

## Architecture

### Micro-Frontend Pattern
```
┌─────────────────────────────────────────┐
│         Shell Application                │
│     (admin-portal-shell)                 │
│  - Routing                               │
│  - Layout (Sidebar)                      │
│  - Pilet orchestration                   │
└─────────────────────────────────────────┘
              ▲
              │ Loads pilets dynamically
              │
┌─────────────┴───────────────┬───────────┐
│  Pilet 1                    │  Pilet N   │
│  (http-tester-pilet)        │  (future)  │
│  - HTTP testing UI          │            │
│  - Independent deployment   │            │
└─────────────────────────────┴────────────┘
```

### Technology Choices
- **Piral**: Micro-frontend framework for pilet orchestration
- **Nx**: Monorepo management and build orchestration
- **Vite**: Fast build tool (version 6.4.1)
- **React 18**: UI library
- **shadcn/ui**: Component library
- **Tailwind CSS**: Styling

---

## Tech Stack

### Core Framework
- **Nx**: 20.4.3 (Monorepo manager)
- **Piral**: 1.9.2 (Micro-frontend orchestration)
- **React**: 18.2.0
- **TypeScript**: ^5.3.2

### Build Tools
- **Vite**: 6.4.1 (Shell and pilet bundler)
- **piral-cli**: ^1.5.0
- **piral-cli-vite6**: ^1.1.3

### UI Framework
- **shadcn/ui**: Component library (Sidebar, Button, etc.)
- **Tailwind CSS**: Utility-first styling
- **Radix UI**: Headless UI primitives
- **lucide-react**: Icon library

### Development Tools
- **Playwright**: Browser automation for debugging
- **MCP Servers**: GitHub, Docker, Chrome DevTools, Playwright (configured in .vscode/settings.json)

### Routing
- **react-router-dom**: v6 (Client-side routing)

---

## Project Structure

```
micro-frontend/
├── .vscode/
│   ├── settings.json           # MCP server configurations
│   └── extensions.json         # Recommended VS Code extensions
│
├── packages/
│   ├── shell/                  # Shell Application
│   │   ├── src/
│   │   │   ├── components/
│   │   │   │   ├── ui/
│   │   │   │   │   └── sidebar.tsx        # shadcn sidebar component
│   │   │   │   ├── AdminLayout.tsx        # Main layout with sidebar
│   │   │   │   └── ErrorInfo.tsx          # Error boundary UI
│   │   │   ├── pages/
│   │   │   │   ├── Dashboard.tsx
│   │   │   │   ├── Users.tsx
│   │   │   │   └── Settings.tsx
│   │   │   ├── layout.tsx                 # Piral layout components
│   │   │   ├── index.tsx                  # Piral instance config
│   │   │   └── index.css                  # Global styles + sidebar theme
│   │   ├── public/
│   │   │   └── index.html
│   │   ├── package.json
│   │   ├── vite.config.ts
│   │   └── tailwind.config.js             # Extended with sidebar tokens
│   │
│   └── pilets/
│       └── http-tester-pilet/  # HTTP Testing Pilet
│           ├── src/
│           │   ├── index.tsx              # Pilet setup function
│           │   └── HttpTester.tsx         # HTTP testing component
│           ├── package.json
│           ├── .piletrc.json              # Pilet configuration
│           └── vite.config.ts
│
├── package.json                # Workspace root scripts
├── nx.json                     # Nx configuration
└── AI_PROJECT_CONTEXT.md       # This file
```

### Important Files

#### Shell Configuration
- `packages/shell/src/index.tsx`: Piral instance creation, pilet loading logic
- `packages/shell/src/layout.tsx`: Layout components (AdminLayout wrapper)
- `packages/shell/src/components/AdminLayout.tsx`: Sidebar navigation implementation
- `packages/shell/tailwind.config.js`: Theme with sidebar color tokens

#### Pilet Configuration
- `packages/pilets/http-tester-pilet/package.json`: Pilet metadata and scripts
- `packages/pilets/http-tester-pilet/.piletrc.json`: Pilet build configuration
- `packages/pilets/http-tester-pilet/src/index.tsx`: Pilet setup with registerPage

#### Workspace Configuration
- `.vscode/settings.json`: MCP servers (Playwright, GitHub, Docker, Chrome DevTools)
- `package.json`: Nx workspace scripts (start, build, start:pilet, build:pilet)

---

## Development Workflow

### Starting the Application

#### Shell Only (Currently Working)
```bash
npm start
# Starts shell on http://localhost:1234
# Sidebar navigation works, all routes functional
```

#### Using Chrome DevTools MCP
You can use the Chrome DevTools MCP server to navigate and interact with the application programmatically:

1. **Start the shell application**:
```bash
npm start
```

2. **Use Playwright browser tools** to navigate:
- Navigate to URL: `http://localhost:1234/`
- Click elements, fill forms, take screenshots
- Inspect console logs and network requests
- Debug application state

**Example Commands**:
- Navigate: "use chrome-devtools mcp to navigate to the main app"
- Inspect: Check browser console for errors and logs
- Interact: Click on links, buttons, fill forms programmatically

#### Shell + Pilet (Currently Blocked)
```bash
# Terminal 1: Start shell
npm start

# Terminal 2: Start pilet in debug mode
npm run start:pilet
```

**⚠️ Current Issue**: Pilet debug mode has compatibility issues with Piral+Vite6+Nx (see Known Issues)

### Building for Production
```bash
# Build shell
npm run build

# Build specific pilet
npm run build:pilet

# Build everything
npm run build:all
```

### Git Workflow
- Repository initialized with 5 commits
- .gitignore configured to include .vscode/settings.json and extensions.json
- Commits made only when explicitly requested by user

---

## Current Status

### ✅ Fully Working
1. **Shell Application**
   - Runs successfully on http://localhost:1234
   - Sidebar navigation with collapsible icon mode
   - Dashboard, Users, Settings pages render correctly
   - Routing works (react-router-dom)
   - Dark/light mode support
   - Error boundaries in place

2. **Pilet Code**
   - http-tester-pilet builds successfully
   - Component code is correct
   - Proper Piral setup function implemented

3. **Development Environment**
   - Nx workspace configured
   - MCP servers for debugging (Playwright, GitHub, Docker, Chrome DevTools)
   - Git repository initialized
   - VS Code settings committed

### ⏸️ Blocked/In Progress
1. **Dynamic Pilet Loading**
   - **Status**: Blocked by Piral+Vite6+Nx compatibility
   - **Issue**: `/$pilet-api` emulator feed endpoint not activating
   - **Attempted Fixes**: 
     - Created .piralrc.json with emulator config
     - Added source field to pilet package.json
     - Configured piralInstances in .piletrc.json
     - Tried multiple port configurations
   - **Root Cause**: Piral CLI emulator service incompatible with Vite6 in Nx monorepo

2. **Pilet Debug Server**
   - Pilet builds successfully but cannot connect to shell
   - Port conflicts: pilet tries 1234, then moves to random ports (50412, 64146, etc.)
   - `pilet debug http://localhost:1234` creates new server instead of connecting

### 🔍 Debugging Performed
- Used Playwright MCP to inspect browser console and network requests
- Confirmed `/$pilet-api` returns HTML (404 page) instead of JSON feed
- Verified `/manage-mock-server` endpoint works (kras mock server running)
- Console error: "Failed to load pilets: Unexpected token '<', "<!DOCTYPE "... is not valid JSON"
- Confirmed issue is framework integration, not user code

---

## Known Issues

### 1. Piral Emulator Feed Not Working (HIGH PRIORITY)
**Problem**: The `/$pilet-api` endpoint that should serve pilet metadata returns HTML instead of JSON.

**Expected Behavior**:
```json
{
  "items": [
    {
      "name": "http-tester-pilet",
      "version": "1.0.0",
      "link": "http://localhost:XXXXX/index.js"
    }
  ]
}
```

**Actual Behavior**: Returns HTML 404 page

**Impact**: Pilets cannot be loaded dynamically during development

**Attempted Solutions**:
1. ❌ Created `packages/shell/.piralrc.json` with emulator config
2. ❌ Added `"source": "src/index.tsx"` to pilet package.json
3. ❌ Configured `piralInstances` in pilet `.piletrc.json`
4. ❌ Multiple port configuration attempts
5. ❌ Various `requestPilets()` implementations

**Current Workaround**: Shell runs without pilets, pilet code maintained separately

**Potential Solutions** (not yet implemented):
1. **Production Build Approach**: Build pilet, create static feed JSON, load from static file
2. **Programmatic Registration**: Use Piral's `registerPilet` API to manually inject pilet
3. **Framework Upgrade/Downgrade**: Try different Vite/Piral versions for compatibility

### 2. Vite CJS Deprecation Warnings
**Problem**: Console warnings about CommonJS build deprecation

**Impact**: Low (warnings only, doesn't break functionality)

**Message Example**:
```
The CJS build of Vite's Node API is deprecated.
```

**Action**: Can be ignored for now, will need migration to ESM in future

### 3. Port Conflicts During Pilet Debug
**Problem**: When running `npm run start:pilet`, pilet tries to use port 1234 (shell's port), then moves to random port

**Impact**: Prevents pilet from connecting to shell properly

**Behavior**:
- Pilet detects port 1234 in use
- Automatically switches to random port (50412, 64146, 57804, etc.)
- `--port` flag not respected by pilet debug command

---

## Key Configuration Files

### 1. Shell Piral Instance (`packages/shell/src/index.tsx`)

**Purpose**: Creates Piral instance and configures pilet loading

**Current Implementation**:
```tsx
const instance = createInstance({
  state: {
    components: layout,
    errorComponents: errors,
  },
  plugins: [],
  requestPilets() {
    // Attempts to fetch from /$pilet-api emulator endpoint
    return fetch('/$pilet-api')
      .then(res => {
        if (!res.ok) {
          console.warn('No pilets available - run "npm run start:pilet" to load pilets');
          return [];
        }
        return res.json();
      })
      .then(data => {
        console.log('Pilets loaded:', data);
        return data.items || [];
      })
      .catch(err => {
        console.warn('Failed to load pilets:', err.message);
        return [];
      });
  },
});
```

**Issue**: `/$pilet-api` endpoint not available (returns HTML 404)

### 2. Pilet Configuration (`packages/pilets/http-tester-pilet/.piletrc.json`)

```json
{
  "schema": "v1",
  "externals": [
    "admin-portal-shell/**"
  ]
}
```

**Purpose**: Declares shell as external dependency to avoid bundling

### 3. Pilet Package.json (`packages/pilets/http-tester-pilet/package.json`)

**Key Fields**:
```json
{
  "scripts": {
    "start": "pilet debug http://localhost:1234",
    "build": "pilet build"
  },
  "piral": {
    "name": "http-tester-pilet",
    "tooling": "1.9.2"
  },
  "devDependencies": {
    "admin-portal-shell": "file:../../shell",
    "piral-cli-vite6": "^1.1.3"
  }
}
```

### 4. MCP Servers (`.vscode/settings.json`)

**Configured Tools**:
```json
{
  "mcpServers": {
    "playwright": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-playwright"]
    },
    "github": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"]
    },
    "mcp-server-docker": {
      "command": "npx",
      "args": ["-y", "@flux159/mcp-server-docker"]
    },
    "chrome-devtools": {
      "command": "npx",
      "args": ["-y", "@automatalabs/mcp-server-chrome"]
    }
  }
}
```

**Purpose**: Enable AI agent to use browser automation, GitHub integration, Docker management, and Chrome DevTools

### 5. Tailwind Configuration (`packages/shell/tailwind.config.js`)

**Extended Theme**:
```javascript
extend: {
  colors: {
    sidebar: {
      DEFAULT: "hsl(var(--sidebar-background))",
      foreground: "hsl(var(--sidebar-foreground))",
      primary: "hsl(var(--sidebar-primary))",
      "primary-foreground": "hsl(var(--sidebar-primary-foreground))",
      accent: "hsl(var(--sidebar-accent))",
      "accent-foreground": "hsl(var(--sidebar-accent-foreground))",
      border: "hsl(var(--sidebar-border))",
      ring: "hsl(var(--sidebar-ring))"
    }
  }
}
```

**CSS Variables** (in `packages/shell/src/index.css`):
```css
:root {
  --sidebar-background: 0 0% 98%;
  --sidebar-foreground: 240 5.3% 26.1%;
  /* ... more variables ... */
}
.dark {
  --sidebar-background: 240 5.9% 10%;
  --sidebar-foreground: 240 4.8% 95.9%;
  /* ... more variables ... */
}
```

---

## MCP Servers

### Configured MCP Tools
The project is configured with Model Context Protocol servers for enhanced development capabilities:

1. **Playwright MCP** (`@modelcontextprotocol/server-playwright`)
   - Browser automation for debugging
   - Used extensively to diagnose pilet loading issues
   - Can navigate, inspect console, network requests

2. **GitHub MCP** (`@modelcontextprotocol/server-github`)
   - Repository operations
   - Pull request management
   - Issue tracking

3. **Docker MCP** (`@flux159/mcp-server-docker`)
   - Container management
   - Image operations
   - Useful for potential containerization

4. **Chrome DevTools MCP** (`@automatalabs/mcp-server-chrome`)
   - Advanced debugging capabilities
   - Performance profiling
   - Network inspection

### Activation
MCP servers are automatically available when working in VS Code with GitHub Copilot. Configuration is in `.vscode/settings.json`.

---

## Next Steps

### Immediate Priorities

#### Option 1: Production Build Approach (Recommended for Quick Progress)
1. Build pilet: `npm run build:pilet`
2. Create static feed JSON file with pilet metadata
3. Update `requestPilets()` to load from static file
4. Test pilet loads in shell

**Pros**: Proven approach, works with current setup  
**Cons**: No hot-reload during pilet development

#### Option 2: Programmatic Registration (Best for Development DX)
1. Import pilet setup function directly in shell
2. Use Piral's `registerPilet` API to inject pilet programmatically
3. Bypass emulator feed service entirely

**Pros**: Fast development, hot-reload works  
**Cons**: Less realistic production simulation

#### Option 3: Fix Emulator Service (Most Complex)
1. Deep dive into Piral CLI source code
2. Investigate Vite6 plugin compatibility
3. Create custom Vite plugin for emulator service
4. Potentially contribute fix upstream

**Pros**: Proper solution, helps community  
**Cons**: Time-consuming, requires framework expertise

### Future Enhancements
1. Add more pilets (e.g., User Management, Analytics Dashboard)
2. Implement authentication/authorization
3. Add state management (Zustand or Redux)
4. Set up CI/CD pipeline
5. Implement E2E tests with Playwright
6. Add monitoring and logging
7. Create pilet deployment pipeline
8. Implement feature flags

### Code Cleanup
- Remove debug console.log statements once pilets loading
- Update comments to reflect final implementation
- Add JSDoc comments to key functions
- Create component documentation

---

## Commands Reference

### Shell Development
```bash
npm start                # Start shell (http://localhost:1234)
npm run build            # Build shell for production
nx run shell:validate    # Run validation checks
```

### Pilet Development
```bash
npm run start:pilet      # Start pilet in debug mode (currently blocked)
npm run build:pilet      # Build pilet for production
```

### Workspace Operations
```bash
npm run build:all        # Build all packages
nx graph                 # View dependency graph
nx reset                 # Clear Nx cache
```

### Git Operations
```bash
git status               # Check current state
git add .                # Stage changes
git commit -m "message"  # Commit (only when user requests)
```

---

## Debugging Notes

### Browser Console Access
When debugging browser issues without direct console access:
1. Use Playwright MCP: `mcp_playwright_browser_navigate` to load page
2. Inspect console: `mcp_playwright_browser_console_messages`
3. Run code in browser: `mcp_playwright_browser_run_code`
4. Take snapshots: `mcp_playwright_browser_snapshot`

### Network Inspection
To check API endpoints:
```typescript
// In Playwright browser context
await page.evaluate(async () => {
  const response = await fetch('/$pilet-api');
  return {
    status: response.status,
    contentType: response.headers.get('content-type'),
    body: await response.text()
  };
});
```

### Common Error Patterns
1. **"Failed to load pilets: Unexpected token '<'"**: `/$pilet-api` returning HTML instead of JSON
2. **"The selected port is already used"**: Port conflict, need to kill existing process
3. **"No valid entry file for the pilet found"**: Missing "source" field in pilet package.json (fixed)
4. **"The defined Piral instance could not be found"**: Missing piralInstances in .piletrc.json (fixed)

---

## Architecture Decisions

### Why Piral?
- Battle-tested micro-frontend framework
- Built-in pilet orchestration
- Supports independent deployment of features
- Shared dependencies to avoid duplication

### Why Nx?
- Excellent monorepo support
- Caching and build optimization
- Task orchestration
- Scalable for multiple packages

### Why Vite?
- Fast HMR during development
- Modern build tool with ES modules
- Better DX than Webpack
- Native TypeScript support

### Why shadcn/ui?
- Copy-paste component model (no external dependency)
- Built on Radix UI (accessible)
- Customizable with Tailwind
- Modern, clean design

---

## Contributing Guidelines (for AI Agents)

### Before Making Changes
1. Read this document completely
2. Check current status in "Current Status" section
3. Review "Known Issues" to avoid duplicate work
4. Read modified files to understand current state

### When Implementing Features
1. Follow existing patterns (e.g., sidebar component structure)
2. Update Tailwind config if adding new theme tokens
3. Test changes in browser (use Playwright MCP if needed)
4. Update this document if architecture changes

### Git Commit Strategy
- Only commit when explicitly requested by user
- Write clear, descriptive commit messages
- Stage only relevant files

### Code Style
- Use TypeScript strict mode
- Follow existing naming conventions
- Add types for all props and functions
- Keep components small and focused

---

## Troubleshooting

### Shell Won't Start
```bash
# Check if port 1234 is in use
Get-NetTCPConnection -LocalPort 1234

# Kill process on port 1234
Get-NetTCPConnection -LocalPort 1234 | Select-Object -ExpandProperty OwningProcess | ForEach-Object { Stop-Process -Id $_ -Force }
```

### Pilet Build Fails
1. Check pilet package.json has correct scripts
2. Verify admin-portal-shell dependency resolves
3. Run from pilet directory: `cd packages/pilets/http-tester-pilet && npm run build`

### Nx Cache Issues
```bash
nx reset
```

### TypeScript Errors
```bash
# Check for type errors
npx tsc --noEmit
```

---

## Lessons Learned

1. **Framework Compatibility Matters**: Not all tools work well together out of the box (Piral+Vite6+Nx)
2. **Browser Automation is Essential**: When console access is limited, Playwright MCP is invaluable
3. **Documentation is Critical**: Complex monorepo setups need thorough documentation
4. **Incremental Progress**: Shell working without pilets is still valuable progress
5. **Multiple Solutions**: When blocked, having alternative approaches (build vs programmatic vs emulator) is helpful

---

## Questions for Human Developer

When continuing this project, consider:

1. **Pilet Loading Strategy**: Which approach do you prefer?
   - Production build with static feed (fast to implement)
   - Programmatic registration (best DX)
   - Fix emulator service (proper but complex)

2. **Authentication**: Will pilets need authentication? Where should it live?

3. **State Management**: Do pilets need shared state? Consider Piral's built-in state or add Redux/Zustand

4. **Deployment**: How will pilets be deployed? CDN, static hosting, or API?

5. **Testing**: E2E tests with Playwright or unit tests with Vitest?

---

## Resources

### Documentation
- [Piral Documentation](https://docs.piral.io)
- [Nx Documentation](https://nx.dev)
- [shadcn/ui Components](https://ui.shadcn.com)
- [Vite Documentation](https://vitejs.dev)

### Community
- [Piral GitHub](https://github.com/smapiot/piral)
- [Nx Discord](https://discord.gg/nx)

### Related Files in This Repo
- `package.json`: Workspace scripts
- `.vscode/settings.json`: Development tool configuration
- `nx.json`: Nx workspace configuration
- `packages/shell/README.md`: Shell-specific documentation (if exists)

---

**Note to AI Agents**: This document should be updated whenever significant changes are made to the project structure, architecture, or known issues. Keep it current to help future agents understand the project state quickly.
