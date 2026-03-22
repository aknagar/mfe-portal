---
description: 'Use these guidelines when testing project locally.'
applyTo: frontend/**
---
# Frontend Testing Guidelines

## Local Development
1. Install: `cd frontend/shell && npm install`
2. Build: `cd frontend/shell && npm run build`
3. Run dev server: `cd frontend/shell && npm start` (http://localhost:1234)
4. Run via Aspire: `dotnet run --project backend/MfePortal.AppHost/MfePortal.AppHost.csproj`

## E2E Tests (Playwright)
```bash
cd frontend && npx playwright test
```
- Base URL: http://localhost:1234 (frontend must be running)
- Config: `frontend/playwright.config.ts`
- Tests: `frontend/tests/e2e/`

## Debugging
- Aspire Dashboard: https://localhost:15002
- Frontend port: 1234 (dev), 80 (Azure Container Apps)
- See [frontend/docs/DEBUG.md](../../frontend/docs/DEBUG.md) for details
