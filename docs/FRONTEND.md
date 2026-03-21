# Build System Prompt for Micro-Frontend Repository

## Quick Context

This is an **Nx monorepo** using **Piral** micro-frontend framework with **Vite** as the bundler. The repository contains:
- **Shell**: Central admin portal application (packages/shell)
- **Pilets**: Independent micro-frontend modules (packages/pilets/*)

Both use React 18, TypeScript, and Tailwind CSS with shadcn/ui components.

---

## Build System Overview

| Tool | Purpose |
|------|---------|
| **Nx** | Monorepo orchestration, task execution, caching |
| **Piral** | Micro-frontend framework, pilet loading/registration |
| **Vite** | Fast bundler for development and production builds |
| **piral-cli** | Piral-specific CLI for shell and pilet builds |

---

## Available Build Commands

### Shell Commands
All executed from repository root with `npm` or `nx run shell:TARGET`

| Command | Purpose | Output |
|---------|---------|--------|
| `npm start` | Start shell dev server | Runs on http://localhost:1234 |
| `npm run build` | Build shell for production | `packages/shell/dist/release/` |
| `npm run validate` | Validate shell configuration | Reports issues, no output if valid |

### Pilet Commands
Execute from individual pilet directory or use `nx` prefix

| Command | Purpose | Output |
|---------|---------|--------|
| `npm start` (in pilet) | Start pilet debug server | Runs on random port, attempts to inject into shell |
| `npm run build` (in pilet) | Build pilet for production | `dist/index.js` bundle (UMD format) |

### Workspace Commands
Execute from repository root

| Command | Purpose |
|---------|---------|
| `npm run build:all` | Build shell + all pilets in sequence |
| `npm run build:hello-world` | Build only hello-world pilet |
| `npm run build:url-getter` | Build only url-getter pilet |

---

## Build Decision Tree

### "I want to start developing the shell"
```
Execute: npm start
Expected: Shell loads on http://localhost:1234
Expected: Shows Dashboard, Users, Settings, Auth pages
Known Issue: Pilet loading will fail (dynamic pilet API endpoint broken)
Workaround: See "Known Issues" section
```

### "I want to work on a specific pilet (e.g., hello-world)"
```
Execute: npm start (from packages/pilets/hello-world-pilet/)
Expected: Pilet dev server starts on random port
Expected: Pilet injects into shell at localhost:1234
Known Issue: Pilet injection may fail due to API endpoint issue
Workaround: See "Known Issues" section
```

### "I want to build everything for production"
```
Execute: npm run build:all
Steps:
  1. Builds shell → packages/shell/dist/release/
  2. Builds hello-world pilet → packages/pilets/hello-world-pilet/dist/index.js
  3. Builds url-getter pilet → packages/pilets/url-getter-pilet/dist/index.js
Expected: All artifacts ready for deployment
Output files are minified and optimized
```

### "I want to build only the shell"
```
Execute: npm run build
Or: nx run shell:build
Expected: Shell bundle in packages/shell/dist/release/
```

### "I want to build only one pilet"
```
Execute: npm run build:hello-world
Or: cd packages/pilets/hello-world-pilet && npm run build
Expected: Pilet bundle in dist/index.js
```

### "I want to validate the shell configuration"
```
Execute: npm run validate
Or: nx run shell:validate
Expected: No output if valid, or detailed error messages
Purpose: Verifies pilet metadata, exports, and configuration
```

---

## Build Output Artifacts

### Shell Build Output
**Location**: `packages/shell/dist/release/`

| File | Purpose |
|------|---------|
| `index.html` | Main HTML entry point |
| `index.*.js` | Shell bundle (hashed filename) |
| `*.css` | Compiled styles |

### Pilet Build Output
**Location**: `packages/pilets/{pilet-name}/dist/`

| File | Purpose |
|------|---------|
| `index.js` | Pilet bundle (UMD module) |
| `package.json` | Pilet metadata (version, name, etc.) |

---

## Project Structure for Building

```
packages/
├── shell/
│   ├── src/
│   │   ├── index.tsx          # Piral instance configuration
│   │   ├── layout.tsx         # Shell layout wrapper
│   │   ├── pages/             # Shell pages (Dashboard, Users, Settings, Auth)
│   │   ├── components/        # UI components available to pilets
│   │   │   └── ui/            # shadcn/ui button, card, input, etc.
│   │   └── lib/               # Utilities
│   ├── package.json           # Shell config: routes, pilet list, exports
│   ├── project.json           # Nx targets (start, build, validate)
│   ├── tsconfig.json          # TypeScript config
│   ├── vite.config.ts         # Vite bundler config
│   └── tailwind.config.js     # Tailwind CSS config
│
└── pilets/
    ├── hello-world-pilet/
    │   ├── src/
    │   │   ├── index.tsx       # Pilet registration point
    │   │   └── HelloWorld.tsx  # Main component
    │   ├── package.json        # Pilet metadata
    │   ├── project.json        # Nx targets
    │   ├── vite.config.ts      # Build config (marks shell deps as external)
    │   └── tsconfig.json
    │
    └── url-getter-pilet/
        ├── src/
        │   ├── index.tsx       # Pilet registration point
        │   └── UrlGetter.tsx   # Main component
        ├── package.json
        ├── project.json
        ├── vite.config.ts
        └── tsconfig.json
```

**Key Files for Building:**

| File | Purpose |
|------|---------|
| `nx.json` | Monorepo config, caching rules, target defaults |
| `packages/shell/package.json` | Shell routes, pilet definitions, shared exports |
| `packages/*/project.json` | Nx targets (start, build, validate) |
| `packages/*/vite.config.ts` | Bundler config, external dependencies marking |
| `packages/*/tsconfig.json` | TypeScript compilation rules |

---

## Key Build Configuration Details

### Nx Caching
- **Production builds** (`build` target): Caching ENABLED for faster rebuilds
- **Dev servers** (`start` target): Caching DISABLED (fresh state required)
- **Inputs**: Production builds exclude test files (*.spec.ts, *.test.ts)

### Vite Bundling
Both shell and pilets use Vite 6 with Rollup. Pilets mark shell dependencies as external:
```typescript
// Prevents bundling of shell exports and React/Router into pilet
external: [
  /^portal-shell\/.*/,
  'react',
  'react-dom',
  'react-router',
  'react-router-dom'
]
```

### Pilet Registration (shell/package.json)
```json
"pilets": {
  "files": ["packages/pilets/*/dist/index.js"],
  "bundles": {
    "hello-world": "/pilets/hello-world-pilet.js",
    "url-getter": "/pilets/url-getter-pilet.js"
  }
}
```

---

## Known Issues & Troubleshooting

### Issue 1: Pilet Dynamic Loading Fails (⚠️ HIGH PRIORITY)

**Symptom**: Shell loads but shows no pilets; console error about `/$pilet-api` endpoint returning HTML instead of JSON.

**Root Cause**: Piral CLI v1.5.0 emulator incompatibility with Vite 6 in Nx monorepo setup.

**Workaround A - Static Pilet Feed** (Recommended):
```
1. Manually create packages/shell/dist/emulator/pilet-api.json with pilet metadata
2. Point shell to load from this static JSON instead of dynamic endpoint
3. Update shell build process to generate this file
```

**Workaround B - Programmatic Registration**:
```
1. Modify shell/src/index.tsx to register pilets directly
2. Import and call piral.registerModule() for each pilet
3. Requires shell rebuild when pilets change
```

**Workaround C - Version Update**:
```
1. Upgrade to newer Piral CLI version
2. Requires testing for compatibility
3. May need to update Vite config
```

### Issue 2: Port Conflicts on Pilet Debug

**Symptom**: Starting pilet debug server fails with "Port already in use" or automatically switches to random port.

**Cause**: Multiple pilet servers trying to use same default port.

**Solution**:
```
Specify port in pilet's vite.config.ts or via CLI:
npm start -- --port 5174
```

### Issue 3: CJS Deprecation Warnings (⚠️ Non-blocking)

**Symptom**: Build or dev server outputs warnings about CommonJS modules in Vite 6.

**Cause**: Some dependencies export CommonJS modules; Vite prefers ESM.

**Impact**: None on functionality; warnings only in console.

**Solution**: Ignore until dependencies update, or suppress with Vite config.

### Issue 4: TypeScript Errors in Pilet Components

**Symptom**: TypeScript compiler errors when importing shell components in pilets.

**Cause**: Path aliases or shared component types not resolved correctly.

**Solution**:
```
1. Verify tsconfig.json paths are correct
2. Check pilet can access portal-shell type definitions
3. Ensure shell exports component types correctly
```

---

## Development Workflow

### Initial Setup
```bash
npm install              # Install all dependencies
npm run validate        # Check shell configuration
npm start              # Start shell dev server
```

### Adding a New Pilet
```bash
cd packages/pilets
# Create new pilet structure matching hello-world-pilet
# Update shell/package.json "pilets" section
npm run build:all      # Build everything to verify
```

### Publishing Artifacts
```bash
npm run build:all                           # Build all projects
# Upload packages/shell/dist/release/* to web server
# Upload packages/pilets/*/dist/index.js to CDN or static host
# Shell loads pilets via <script> tags with versioned paths
```

### Testing Before Publish
```bash
npm start              # Start shell on localhost:1234
# Manually verify all pages load
# Verify pilets display (once API issue resolved)
npm run validate       # Check configuration
```

---

## When to Use Each Command

| Scenario | Command | Why |
|----------|---------|-----|
| **Local development of shell** | `npm start` | Loads shell with hot reload on localhost:1234 |
| **Local development of pilet** | `npm start` (from pilet dir) | Injects pilet into running shell for live testing |
| **Before committing code** | `npm run validate` | Catches config issues early |
| **Building for staging** | `npm run build:all` | Produces all artifacts in one step |
| **Building for production** | `npm run build:all` | Same artifacts; update deployment scripts to upload |
| **CI/CD pipeline** | `npm run build:all` | Atomic, reproducible build of all projects |

---

## Environment & Dependencies

### Core Technologies
- **Node.js**: v18+ (check .nvmrc if present)
- **npm**: v9+ 
- **React**: 18.2.0
- **TypeScript**: 5.3.2+
- **Tailwind CSS**: 3.4.0
- **shadcn/ui**: For pre-built accessible components

### Build Tools
- **Nx**: 20.4.3
- **Vite**: 6.4.1
- **piral**: 1.9.2
- **piral-cli**: 1.5.0
- **piral-cli-vite6**: 1.1.3

### Verify Environment
```bash
node --version        # Should be v18+
npm --version         # Should be v9+
npx nx --version      # Should be 20.4.3
```

---

## Additional Resources

- **[AI_PROJECT_CONTEXT.md](AI_PROJECT_CONTEXT.md)**: Full project overview, architecture, and known limitations
- **[packages/shell/AUTHENTICATION.md](packages/shell/AUTHENTICATION.md)**: Azure AD setup and authentication configuration
- **Nx Documentation**: https://nx.dev/docs
- **Piral Documentation**: https://docs.piral.io/
- **Vite Documentation**: https://vitejs.dev/

---

## Using This Prompt

**For AI Assistants/Copilot:**
When asked to build, debug, or modify the project:
1. Reference this document for available commands
2. Verify you're running commands from the correct directory
3. Check the troubleshooting section for known issues
4. Use the decision tree to determine the right build command

**For Developers:**
- Bookmark this document
- Use the decision tree to find the right command for your task
- Check troubleshooting before reporting issues
- Keep this updated as new pilets are added or build process changes

**For CI/CD:**
Use `npm run build:all` as your primary build command; it's designed for atomic, reproducible builds of all projects.
