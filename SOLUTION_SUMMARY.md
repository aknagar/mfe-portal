# MfePortal Solution Summary

## Project Overview

MfePortal is a production-ready Micro Frontend (MFE) Portal solution built with:
- **Frontend**: Piral micro-frontend framework with Nx monorepo
- **Backend**: .NET Aspire with AugmentService microservice
- **Cloud**: Azure Container Apps with Dapr integration
- **Infrastructure**: Bicep IaC with Azure Developer CLI

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│              Piral Shell (Frontend)                      │
│  - Micro-frontend federation                            │
│  - Multiple pilets (hello-world, url-getter)           │
└──────────────────────┬──────────────────────────────────┘
                       │
                       │ HTTPS
                       ▼
┌─────────────────────────────────────────────────────────┐
│          .NET Aspire (Process-based or Azure)           │
│                                                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │  AugmentService                                  │  │
│  │  - HTTP/HTTPS reverse proxy                      │  │
│  │  - OpenAPI/Swagger documentation                │  │
│  │  - Health checks (/health, /alive)              │  │
│  │  - Dapr sidecar enabled (production)            │  │
│  └──────────────────────────────────────────────────┘  │
│                      │                                   │
│                      ▼                                   │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Dapr Runtime (Local or Azure Container Apps)   │  │
│  │  - State Management (Redis / Azure Cosmos)      │  │
│  │  - Pub/Sub Messaging (Redis / Azure Service Bus)│  │
│  │  - Service Invocation                           │  │
│  │  - Output Bindings                              │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

## Repository Structure

```
mfe-portal/
├── backend/                          # .NET Backend Services
│   ├── AugmentService/              # Reverse Proxy Microservice
│   │   ├── Program.cs               # Service configuration
│   │   ├── AugmentService.csproj    # Project file (includes Dapr.Client)
│   │   ├── API_DOCUMENTATION.md     # API reference
│   │   └── Dockerfile               # Container image definition
│   ├── MfePortal.AppHost/           # Aspire Orchestrator
│   │   └── Program.cs               # Service orchestration
│   ├── MfePortal.ServiceDefaults/   # Shared service configuration
│   ├── infra/                        # Azure Infrastructure as Code
	│   │   ├── main.bicep               # Entry point orchestrator template
	│   │   ├── infra.bicep              # Core resources (ACR, CAE, Redis, LAW)
	│   │   ├── container-app.bicep      # AugmentService with Dapr
│   │   ├── parameters.json          # Deployment parameters
│   │   ├── dapr-components.yaml     # Dapr component config
│   │   └── DEPLOYMENT.md            # Azure deployment guide
│   ├── dapr/
│   │   └── components/              # Local Dapr components
│   │       ├── statestore.yaml      # Redis state store
│   │       └── pubsub.yaml          # Redis pub/sub
│   ├── azure.yaml                   # Azure Developer CLI config
│   ├── DAPR_SETUP.md               # Local Dapr setup guide
│   ├── TESTING.md                   # Testing guide
│   ├── test-local.ps1               # Automated test script
│   ├── PREFERENCES.md               # HTTPS-only security policy
│   └── README.md                    # Backend overview
├── packages/
│   ├── pilets/                      # Micro-frontends
│   │   ├── hello-world-pilet/
│   │   └── url-getter-pilet/
│   └── shell/                       # Piral shell application
├── .github/
│   └── skills/                      # Claude AI skill definitions
├── package.json                     # Root npm configuration
├── nx.json                          # Nx monorepo configuration
└── README.md                        # Project overview
```

## Key Features

### Security
- ✅ HTTPS-only communications
- ✅ Certificate validation (TLS 1.2+)
- ✅ Managed identities in Azure
- ✅ Secret management via Key Vault

### Observability
- ✅ Health check endpoints
- ✅ OpenAPI/Swagger documentation
- ✅ Dapr distributed tracing
- ✅ Application Insights integration
- ✅ Log Analytics centralized logging

### Scalability
- ✅ Horizontal auto-scaling (Container Apps)
- ✅ Load-balanced reverse proxy
- ✅ Distributed state management via Dapr
- ✅ Pub/Sub message broker

### Developer Experience
- ✅ Local development with process-based Aspire
- ✅ One-command Azure deployment with azd
- ✅ Infrastructure as Code (Bicep)
- ✅ Comprehensive testing guides
- ✅ API documentation with Swagger/Scalar

## Getting Started

### Local Development

1. **Start AugmentService**:
   ```bash
   cd backend/AugmentService
   dotnet run
   ```
   Service available at `https://localhost:7139`

2. **Run Tests**:
   ```bash
   cd backend
   pwsh ./test-local.ps1
   ```

3. **(Optional) Start with Dapr**:
   ```bash
   # Prerequisites: Docker, Redis, Dapr CLI
   docker run -d --name dapr-redis -p 6379:6379 redis:7-alpine
   dapr init --slim
   
   # Run with Dapr sidecar
   cd backend/AugmentService
   dapr run --app-id augmentservice \
     --app-port 7139 \
     --dapr-http-port 3500 \
     --dapr-grpc-port 50001 \
     --components-path ../dapr/components \
     -- dotnet run
   ```

### Azure Deployment

1. **Install Azure Developer CLI**:
   ```bash
   choco install azure-dev  # Windows
   brew install azure-dev   # macOS
   ```

2. **Initialize and Deploy**:
   ```bash
   cd backend
   azd auth login
   azd env new
   azd provision
   azd deploy
   ```

## API Endpoints

### Application APIs

| Endpoint | Version | Description |
|----------|---------|-------------|
| `/proxy` | v1 | Reverse proxy for external URLs |
| `/swagger` | v1 | Interactive API documentation (Swagger UI) |
| `/openapi/v1.json` | v1 | OpenAPI 3.0.1 specification |

### System APIs

| Endpoint | Version | Description |
|----------|---------|-------------|
| `/health` | v1 | Comprehensive health check |
| `/alive` | v1 | Kubernetes liveness probe |

### Base URL

- **Development**: `https://localhost:7139`
- **Production**: Deployed via Azure Container Apps (azd provides URL)

## Documentation

- **[API_DOCUMENTATION.md](backend/AugmentService/API_DOCUMENTATION.md)** - API reference
- **[DAPR_SETUP.md](backend/DAPR_SETUP.md)** - Local Dapr integration guide
- **[TESTING.md](backend/TESTING.md)** - Comprehensive testing guide
- **[DEPLOYMENT.md](backend/infra/DEPLOYMENT.md)** - Azure deployment guide
- **[PREFERENCES.md](backend/PREFERENCES.md)** - Security and deployment preferences

## Technology Stack

### Backend
- .NET 9.0
- ASP.NET Core Minimal APIs
- OpenAPI/Swagger
- Dapr 1.14+
- Azure Container Apps
- Azure Cache for Redis

### Infrastructure
- Bicep IaC
- Azure Developer CLI (azd)
- Azure Container Registry
- Azure Log Analytics
- Azure Managed Environment

### Frontend
- Piral micro-frontend framework
- Nx monorepo
- Vite bundler
- React/TypeScript
- Tailwind CSS

## Build & Deploy

### Local Build
```bash
# Backend
cd backend/AugmentService
dotnet build

# Frontend
npm install
npm run build
```

### Container Build
```bash
cd backend
docker build -f AugmentService/Dockerfile -t augmentservice:latest .
```

### Azure Deployment
```bash
cd backend
azd up  # Provision and deploy in one command
```

## Monitoring & Logging

### Health Checks
```bash
# Comprehensive health check
curl -k https://localhost:7139/health

# Liveness probe
curl -k https://localhost:7139/alive
```

### Logs

Local:
```bash
# View AugmentService logs in console output
dotnet run
```

Azure:
```bash
# View logs in Log Analytics
az monitor log-analytics query \
  --workspace <id> \
  --analytics-query "ContainerAppConsoleLogs_CL | where TimeGenerated > ago(1h)"
```

## Troubleshooting

### Service Won't Start
1. Check port 7139 availability
2. Verify .NET 9.0 is installed
3. Check certificate in `launchSettings.json`

### Tests Failing
1. Ensure service is running on port 7139
2. Check HTTPS certificate settings
3. Review logs in test script output

### Dapr Connection Issues
1. Verify Redis is running
2. Check Dapr sidecar is initialized
3. Review component configuration in `dapr/components/`

See [TESTING.md](backend/TESTING.md#troubleshooting) for detailed troubleshooting.

## Branching Strategy

- **main**: Stable production code
- **full-stack**: Integration branch with all features
- **daprize**: Active development branch for Dapr integration

## Next Steps

1. **Frontend Integration**: Connect Piral shell to AugmentService proxy
2. **Extended Dapr Features**: Implement state endpoints and pub/sub handlers
3. **CI/CD Pipeline**: Set up GitHub Actions for automated deployments
4. **Custom Domains**: Configure DNS and SSL certificates
5. **Multi-Region**: Deploy to multiple Azure regions for HA

## Contributing

1. Create feature branch from `full-stack`
2. Test locally with provided test scripts
3. Update documentation
4. Create pull request
5. Deploy to Azure for integration testing

## Support

- **Local Development**: See [TESTING.md](backend/TESTING.md)
- **Dapr Issues**: See [DAPR_SETUP.md](backend/DAPR_SETUP.md)
- **Azure Deployment**: See [DEPLOYMENT.md](backend/infra/DEPLOYMENT.md)
- **API Issues**: See [API_DOCUMENTATION.md](backend/AugmentService/API_DOCUMENTATION.md)

## License

[Add your license here]

---

**Last Updated**: December 31, 2025
**Version**: 1.0.0
**Branch**: daprize
