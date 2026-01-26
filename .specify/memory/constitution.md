<!--
Sync Impact Report - Constitution v1.0.0
=====================================
Version: 0.0.0 → 1.0.0 (Initial Constitution)
Date: 2026-01-26

Changes:
- ✅ NEW: Clean Architecture principle
- ✅ NEW: Micro-Frontend Architecture principle
- ✅ NEW: Security-First Development principle
- ✅ NEW: Testing & Quality Assurance principle
- ✅ NEW: Documentation & API Standards principle
- ✅ NEW: Cloud-Native Architecture principle  
- ✅ NEW: Observability & Monitoring principle
- ✅ NEW: Security Requirements section
- ✅ NEW: Development Workflow section

Template Consistency Status:
- ✅ .specify/templates/plan-template.md - Aligned with architecture and security principles
- ✅ .specify/templates/spec-template.md - Aligned with documentation standards
- ✅ .specify/templates/tasks-template.md - Aligned with quality and testing requirements
- ⚠️  Commands in .specify/templates/commands/*.md - Review recommended for agent-specific references

Follow-up Actions:
- None - all principles defined for initial version
-->

# MfePortal Constitution

## Core Principles

### I. Clean Architecture (NON-NEGOTIABLE)
**All backend services MUST follow Clean Architecture with strict layering:**
- **Core Layer**: Pure domain logic, entities, interfaces - ZERO external dependencies
- **Application Layer**: Business logic, services, DTOs - depends ONLY on Core
- **Infrastructure Layer**: Repositories, external integrations - implements Core interfaces
- **API Layer**: Controllers, endpoints - orchestrates Application services

**Dependency Rule**: Dependencies MUST flow inward toward Core. Infrastructure implements Core abstractions, but Core never references Infrastructure.

**Rationale**: Enables independent testing, technology migration, and maintains business logic integrity separate from framework concerns.

### II. Micro-Frontend Architecture
**Frontend MUST be composable, independently deployable micro-frontends:**
- Shell application provides routing, layout, and pilet orchestration
- Pilets (micro-frontends) are independently built, versioned, and deployed
- Each pilet owns its features end-to-end with minimal coupling
- Shared dependencies managed through the shell to avoid duplication
- Technology independence: different pilets MAY use different tech stacks

**Tooling**: Piral for pilet orchestration, Nx for monorepo management, Vite for fast builds.

**Rationale**: Enables parallel team development, independent feature deployment, and technology flexibility without monolith coupling.

### III. Security-First Development (NON-NEGOTIABLE)
**All security measures are mandatory and non-negotiable:**
- **HTTPS-Only**: ALL services MUST run on HTTPS ports; HTTP is DISABLED
- **No Secrets in Git**: Credentials, API keys, tokens MUST NEVER be committed (use .env.local, user-secrets, Key Vault)
- **Managed Identity**: Production MUST use Azure Managed Identity for resource access
- **Pre-commit Validation**: git-secrets or equivalent MUST be configured to prevent secret leaks
- **Immediate Rotation**: Any accidentally committed secret MUST be rotated immediately and removed from git history

**Rationale**: Security breaches damage trust and compliance. HTTPS prevents MITM attacks. Secrets in git are permanent exposure risks.

### IV. Testing & Quality Assurance
**Comprehensive testing is required at every layer:**
- **Unit Tests**: Core and Application layers MUST have unit tests (target: 80%+ coverage)
- **Integration Tests**: Infrastructure layer MUST have integration tests for repositories and external services
- **Health Checks**: ALL services MUST expose `/health` and `/alive` endpoints
- **Automated Testing**: Test scripts MUST be provided and runnable locally before deployment
- **Test-First Recommended**: Write tests before implementation where feasible to validate requirements

**Test environments**: Local (in-memory/Docker), CI/CD (test containers), Production (health checks + monitoring).

**Rationale**: Layered testing catches bugs early, validates business logic independent of infrastructure, and ensures production readiness.

### V. Documentation & API Standards
**Every feature and API MUST be documented:**
- **OpenAPI/Swagger**: ALL APIs MUST expose OpenAPI specifications
- **README Per Component**: Each microservice, pilet, and module MUST have a README
- **Architecture Docs**: Maintain ARCHITECTURE.md for system design decisions
- **Testing Docs**: TESTING.md MUST explain how to run local and automated tests
- **Deployment Guides**: DEPLOYMENT.md MUST cover Azure deployment with azd
- **API Reference**: Generate API documentation from code (XML comments → OpenAPI)

**Rationale**: Documentation is code for humans. APIs without docs are unusable. Architecture docs prevent tribal knowledge loss.

### VI. Cloud-Native Architecture
**Design for Azure cloud-native deployment:**
- **.NET Aspire**: Use for local orchestration and cloud deployment patterns
- **Dapr**: Use for distributed application patterns (state, pub/sub, service invocation)
- **Azure Container Apps**: Target deployment platform with auto-scaling (1-10 replicas)
- **Infrastructure as Code**: Use Bicep templates for all Azure resources
- **Azure Developer CLI (azd)**: Support `azd up` for one-command deployment
- **Managed Services**: Prefer Azure managed services (PostgreSQL Flexible Server, Redis, Service Bus) over self-hosted

**Rationale**: Cloud-native patterns enable scalability, resilience, and operational simplicity. IaC ensures reproducible deployments.

### VII. Observability & Monitoring
**All services MUST be observable and debuggable:**
- **Structured Logging**: Use ILogger with structured data (no string concatenation)
- **OpenTelemetry**: Distributed tracing via .NET Aspire and Dapr
- **Health Endpoints**: `/health` for readiness, `/alive` for liveness probes
- **Metrics**: Expose metrics for monitoring (CPU, memory, request rates, error rates)
- **Error Handling**: Catch exceptions at API boundaries, log with context, return meaningful errors

**Rationale**: Production debugging requires telemetry. Structured logs enable querying. Health checks enable auto-recovery.

## Security Requirements

### Transport Security
- MUST use HTTPS on all ports (TLS 1.2+)
- MUST redirect HTTP to HTTPS if HTTP endpoints exist
- Self-signed certificates acceptable for localhost development

### Secrets Management
- **Local Development**: Use .env.local, docker-compose.override.yml, or dotnet user-secrets
- **CI/CD**: Use GitHub Secrets or Azure DevOps Secrets
- **Production**: Use Azure Key Vault with Managed Identity
- MUST add sensitive files to .gitignore
- MUST scan for secrets before commits using git-secrets or detect-secrets

### Authentication & Authorization
- Production MUST implement authentication (Azure AD, OAuth, etc.)
- API endpoints MUST validate authorization for protected resources
- Use role-based access control (RBAC) where applicable

### Compliance
- Follow OWASP security best practices
- Regular dependency updates for CVE patches
- Audit logs for sensitive operations

## Development Workflow

### Feature Development
1. **Specification First**: Create spec using `/speckit.specify` before coding
2. **Design Planning**: Use `/speckit.plan` to define technical approach
3. **Task Breakdown**: Generate tasks with `/speckit.tasks` before implementation
4. **Branch Strategy**: Feature branches from main, PR-based reviews
5. **Testing**: Run tests locally before pushing: `cd backend && pwsh ./test-local.ps1`

### Code Quality Gates
- All PRs MUST pass automated tests
- Code reviews MUST verify compliance with Clean Architecture layers
- No secrets in code (pre-commit hooks enforced)
- OpenAPI specs MUST be valid and complete
- Documentation MUST be updated for user-facing changes

### CI/CD Pipeline
- Build: Compile all services and pilets
- Test: Run unit, integration, and health check tests
- Security Scan: Detect secrets and vulnerabilities
- Deploy: Use `azd up` for Azure deployments
- Health Validation: Verify `/health` endpoints post-deployment

### Version Control
- Commit messages MUST be clear and descriptive
- Never force-push to main branch
- Squash commits for cleaner history (optional, team decision)
- Tag releases with semantic versioning (e.g., v1.2.3)

## Governance

**This Constitution is the supreme development standard. All practices, patterns, and processes MUST align with these principles.**

### Amendment Process
- Amendments require documentation of rationale and impact
- Version MUST increment per semantic versioning:
  - **MAJOR**: Backward-incompatible principle removals or redefinitions
  - **MINOR**: New principles added or material expansions
  - **PATCH**: Clarifications, wording fixes, non-semantic refinements
- Last Amended date MUST be updated to ISO 8601 format (YYYY-MM-DD)
- Sync Impact Report MUST be added as HTML comment at top of document

### Compliance Verification
- All code reviews MUST verify compliance with architecture and security principles
- Deviations MUST be justified in PR comments and documented
- Complexity MUST be justified; prefer simplicity (YAGNI)
- Use DEVELOPMENT.md for runtime development guidance and best practices

### Exception Handling
- Exceptions require written justification and approval from project lead
- Document exceptions in ADR (Architecture Decision Record) format
- Revisit exceptions periodically to eliminate technical debt

**Version**: 1.0.0 | **Ratified**: 2026-01-26 | **Last Amended**: 2026-01-26
