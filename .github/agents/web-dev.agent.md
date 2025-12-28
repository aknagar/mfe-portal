---
model: gemini-3-pro-preview
preferredModel: gemini-3-pro-preview
---

# Micro-Frontend Admin Portal - AI Coding Guidelines

## Project Overview
This is an **Nx monorepo** implementing a **Piral-based micro-frontend architecture** for a modular admin portal. The shell application orchestrates independently deployable pilets (micro-frontend modules).

**Tech Stack**: React 18, Piral 1.9.2, Nx 20.4.3, Vite 6.4.1, TypeScript, shadcn/ui, Tailwind CSS

## Architecture Essentials

### Micro-Frontend Pattern
```
Shell (admin-portal-shell)
  └─ Hosts routes, layout, authentication
  └─ Programmatically registers pilets
  └─ Provides shared UI components via externals

Pilets (url-getter-pilet, hello-world-pilet)
  └─ Independently developed React components
  └─ Register pages/slots via PiletApi.registerPage()
  └─ Cannot bundle shell code (declared in externals)
```

### Critical Design Decision
Pilets are **programmatically registered** in `packages/shell/src/index.tsx` (not dynamically via feed endpoint). Direct imports from pilet sources work because Nx resolves monorepo paths. This avoids the Piral CLI emulator service incompatibility with Vite 6.

## File Structure & Key Components

| Path | Purpose |
|------|---------|
| `packages/shell/src/index.tsx` | Piral instance creation, pilet registration |
| `packages/shell/src/layout.tsx` | Layout components (Layout, ErrorInfo) |
| `packages/shell/src/components/AdminLayout.tsx` | Sidebar navigation with collapsible icon mode |
| `packages/shell/src/components/ui/` | shadcn/ui components (Button, Sidebar, Card, etc.) |
| `packages/pilets/*/src/index.tsx` | Pilet entry: `export function setup(api: PiletApi)` |
| `packages/shell/.piralrc` | Shell build config for Piral CLI |
| `packages/shell/tailwind.config.js` | Extended with sidebar color tokens |
| `nx.json` | Monorepo config; Vite plugin targets build/serve/test |

## Development Workflows

### Starting the Application
```bash
npm start
# Runs: nx run shell:start → piral debug --port 1234
# Shell available at http://localhost:1234
# Both pilets (hello-world, url-getter) auto-registered
```

### Building Production
```bash
npm run build              # Build shell
npm run build:all         # Build shell + all pilets
npm run build:hello-world # Build specific pilet
```

### Common Commands
- `npm run start:hello-world` - Start hello-world pilet (not needed; already registered)
- `npm run validate` - Validate shell with Piral CLI

## Pilet Development Pattern

**Every pilet follows this pattern:**

```tsx
// packages/pilets/{pilet-name}/src/index.tsx
import type { PiletApi } from 'portal-shell';
import { MyComponent } from './MyComponent';

export function setup(api: PiletApi) {
  api.registerPage('/my-route', MyComponent, { meta: { title: 'My Page' } });
}
```

**Key Points:**
1. Import `PiletApi` type from `portal-shell` (the shell package)
2. Export `setup(api)` function—Piral calls this to initialize pilet
3. Use `api.registerPage()` to add routes to shell
4. Component receives no props from setup; use Piral events/state for communication
5. Do NOT import shell components that create circular dependencies (stick to UI primitives)

## Shell Integration Points

### Adding Shell Routes
Edit `packages/shell/src/index.tsx` → the `requestPilets()` function currently returns empty array. Pilets register their own pages, so shell routes are defined by:
1. Built-in pages in `packages/shell/src/pages/` (Dashboard, Users, Settings, Login, Logout, Auth)
2. Pilet registrations via `api.registerPage()` in pilet setup functions

### Layout & Sidebar
- `AdminLayout` wraps all pages with sidebar navigation
- Navigation config in `AdminLayout.tsx` links to pilet routes (e.g., `/hello-world`, `/api-playground`)
- Sidebar collapses to icon mode; use `lucide-react` icons consistently
- Authentication state visible in footer; uses Azure MSAL

### Component Sharing
Shell exports UI components via `pilets.scripts` in `packages/shell/package.json`:
```json
"scripts": {
  "components/ui/button": "./src/components/ui/button.tsx",
  "components/ui/card": "./src/components/ui/card.tsx"
}
```
Pilets can import: `import { Button } from 'components/ui/button'`

## Critical Implementation Notes

### Styling
- **Tailwind CSS** configured in `packages/shell/`
- Pilets inherit shell's Tailwind config during build
- shadcn/ui component class names must match shell's theme tokens
- Dark/light mode toggles via `theme` provider in layout

### Authentication (Azure MSAL)
- Configured in `packages/shell/src/authConfig.ts`
- `AuthButton` component in `packages/shell/src/components/`
- Pilets receive authenticated user context via Piral state (not yet fully implemented)
- Protected routes would require checking `useMsal()` in pilet components

### TypeScript
- Shell is `sourceRoot` in `nx.json` (default project)
- Each pilet has its own `tsconfig.json`
- Type `PiletApi` from shell defines pilet API contract
- Strict mode enabled; avoid `any` types

## Known Limitations & Workarounds

**Dynamic Pilet Feeds Not Working**: Piral's `/$pilet-api` emulator endpoint incompatible with Vite 6 in Nx. Workaround: Use programmatic registration. If dynamic feeds needed later, build static JSON feed at compile time.

**Port Conflicts**: Pilet debug mode tries to use port 1234 (shell's port). Manually specifying `--port XXXX` in dev often required.

**CJS Deprecation Warnings**: Normal with Vite 6; ignorable for development.

## Best Practices for This Project

1. **Pilet Naming**: Use kebab-case (`hello-world-pilet`); update navigation and sidebar links in AdminLayout
2. **API Types**: Define custom API extensions in shell's type definitions for type-safe pilet communication
3. **Testing**: Playwright configured in workspace; write tests in `tests/` directory with `*.spec.ts` pattern
4. **Git**: Don't commit without explicit user request; `.gitignore` includes `.vscode/`
5. **Debugging**: Use Chrome DevTools MCP server in VS Code to inspect shell at runtime; logs show pilet registration
6. **Error Boundaries**: Shell provides `ErrorInfo` component; pilets can wrap components in Piral's error boundary
7. **Monorepo Discipline**: Avoid circular imports; pilets → shell is OK; shell → pilets only at registration time
