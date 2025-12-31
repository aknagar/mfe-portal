# Azure Deployment View

Comprehensive visualization of the MFE Portal backend infrastructure deployed to Azure Container Apps with Dapr integration.

## Resource Architecture

Complete Azure infrastructure deployed per environment (test/prod):

```mermaid
graph TB
    subgraph Azure["Azure Subscription"]
        subgraph RG["Resource Group: rg-mfe-{test|prod}"]
            subgraph Identity["Identity Management"]
                ManagedID["User-Assigned Managed Identity<br/>Name: id-{token}<br/>Reusable across services<br/>Independent lifecycle"]
            end
            
            subgraph CAE["Container Apps Environment"]
                AugmentService["AugmentService Container App<br/>Image: augmentservice:latest<br/>Port: 8080<br/>Identity: User-Assigned"]
                DaprSidecar["Dapr Sidecar<br/>HTTP: 3500<br/>gRPC: 50001"]
                AugmentService -.->|communicate| DaprSidecar
            end
            
            KeyVault["Azure Key Vault<br/>Secrets: RedisConnectionString<br/>Access: RBAC via Managed Identity"]
            Redis["Azure Cache for Redis<br/>State Store & Pub/Sub<br/>TEST: Basic | PROD: Standard"]
            ACR["Container Registry<br/>Registry Name: {environment}acr<br/>SKU: Basic"]
            LogAnalytics["Log Analytics Workspace<br/>Retention: 30 days<br/>SKU: PerGB2018"]
            
            ManagedID -->|Assigned to| AugmentService
            AugmentService -->|read secret| KeyVault
            KeyVault -->|contains| Redis
            DaprSidecar -->|state/pubsub| Redis
            AugmentService -->|push image| ACR
            AugmentService -->|send logs| LogAnalytics
            Redis -->|store logs| LogAnalytics
        end
    end
    
    style CAE fill:#e1f5ff
    style Identity fill:#c8e6c9
    style KeyVault fill:#c8e6c9
    style RG fill:#f3e5f5
    style Azure fill:#fff3e0
```

## Service Deployment Configuration

AugmentService container configuration and scaling parameters:

```mermaid
graph LR
    Config["Service Configuration"]
    
    Config -->|App Port| Port8080["8080<br/>HTTP API"]
    Config -->|Dapr| DaprConfig["Dapr Sidecar<br/>HTTP: 3500<br/>gRPC: 50001"]
    Config -->|Identity| ManagedID["System-Assigned<br/>Managed Identity"]
    Config -->|HTTPS| HTTPSConfig["Ingress: HTTPS<br/>Allow Insecure: false"]
    Config -->|Environment| ProdEnv["Environment:<br/>Production"]
    
    Scaling["Auto-scaling Rules"]
    Scaling -->|Concurrent Requests| ConcurrentReq["Threshold: 10<br/>requests/replica"]
    Scaling -->|Min Replicas| MinRep["TEST: 1<br/>PROD: 2"]
    Scaling -->|Max Replicas| MaxRep["TEST: 3<br/>PROD: 10"]
    
    Resources["Resource Limits"]
    Resources -->|TEST| TestRes["CPU: 0.5 vCPU<br/>Memory: 1 GiB"]
    Resources -->|PROD| ProdRes["CPU: 1 vCPU<br/>Memory: 2 GiB"]
    
    style Config fill:#c8e6c9
    style Scaling fill:#bbdefb
    style Resources fill:#ffe0b2
```

## Environment-Specific Deployment

Parameter overrides for test and production environments:

```mermaid
graph TB
    Parameters["parameters.json<br/>Base Configuration"]
    
    Parameters -->|TEST Override| TestParams["parameters.test.json<br/>---<br/>Redis: Basic<br/>Compute: 0.5 vCPU/1GB<br/>Replicas: 1-3<br/>Est. Cost: $25-30/mo"]
    
    Parameters -->|PROD Override| ProdParams["parameters.prod.json<br/>---<br/>Redis: Standard<br/>Compute: 1 vCPU/2GB<br/>Replicas: 2-10<br/>Est. Cost: $75-100/mo"]
    
    TestParams -->|Select via| Selection["azd env select test"]
    ProdParams -->|Select via| Selection
    
    Selection -->|Run| Bicep["main.bicep<br/>Orchestration<br/>Creates: Key Vault"]
    Bicep -->|Create Infra| InfraBicep["infrastructure.bicep<br/>Log Analytics, ACR, Redis, CAE<br/>Creates: Key Vault & Secrets"]
    Bicep -->|Deploy Service| ContainerBicep["container-app.bicep<br/>AugmentService Container App<br/>References: Key Vault Secrets"]
    
    style Parameters fill:#f1f8e9
    style TestParams fill:#e8f5e9
    style ProdParams fill:#fce4ec
    style Bicep fill:#e0f2f1
```

## Deployment Flow

Complete deployment pipeline from source to running container:

```mermaid
graph LR
    Dev["Local Development<br/>azd env new"]
    
    Dev -->|Configure| EnvSetup["Environment Setup<br/>azd env select {test|prod}"]
    
    EnvSetup -->|Create Resources| Provision["azd provision<br/>---<br/>Execute main.bicep<br/>Create Azure Resources"]
    
    Provision -->|Output Variables| VarOutput["AZURE_CONTAINER_REGISTRY_ENDPOINT<br/>AZURE_CONTAINER_REGISTRY_NAME<br/>RESOURCE_GROUP<br/>ENVIRONMENT_NAME"]
    
    VarOutput -->|Build & Push| Deploy["azd deploy<br/>---<br/>Build Docker Image<br/>Push to ACR<br/>Update Container App"]
    
    Deploy -->|Verify| Running["Running Service<br/>Container Apps<br/>Dapr Sidecar Active<br/>Redis Connected"]
    
    Running -->|Monitor| Monitor["azd monitor<br/>View logs & metrics<br/>Log Analytics"]
    
    style Provision fill:#c8e6c9
    style Deploy fill:#bbdefb
    style Running fill:#ffe0b2
    style Monitor fill:#f8bbd0
```

## Azure Key Vault Integration

Centralized secrets management for production deployments:

```mermaid
graph TB
    subgraph KeyVaultSetup["Key Vault Setup"]
        KVResource["Azure Key Vault<br/>Name: kv-{token}<br/>SKU: Standard<br/>RBAC Enabled"]
        SecretStorage["Secrets Storage<br/>RedisConnectionString<br/>Encrypted at rest<br/>TLS in transit"]
        
        KVResource -->|Stores| SecretStorage
    end
    
    subgraph AccessControl["Access Control"]
        ManagedIdentity["Container App<br/>System-Assigned Identity<br/>No credentials needed"]
        RoleAssignment["RBAC Role<br/>Key Vault Secrets User<br/>Can: Get, List secrets<br/>Cannot: Delete, Create"]
        
        ManagedIdentity -->|Gets role| RoleAssignment
        RoleAssignment -->|Applies to| KVResource
    end
    
    subgraph Runtime["Runtime Secret Retrieval"]
        ContainerStart["Container Startup<br/>Azure SDK detects<br/>keyVaultUrl reference"]
        SecretFetch["Automatic Secret Fetch<br/>Uses Managed Identity<br/>No password hardcoding"]
        SecretInject["Secret Injection<br/>REDIS_CONNECTION_STRING<br/>Env variable set"]
        
        ContainerStart -->|Triggers| SecretFetch
        SecretFetch -->|Via| KVResource
        KVResource -->|Returns| SecretInject
        SecretInject -->|Used by| DaprApp["Dapr & Application<br/>Connect to Redis"]
    end
    
    AccessControl -->|Enables| Runtime
    
    style KeyVaultSetup fill:#c8e6c9
    style AccessControl fill:#bbdefb
    style Runtime fill:#fff9c4
```

## Data Flow and Communication

How AugmentService communicates with infrastructure components:

```mermaid
graph TB
    API["AugmentService.Api<br/>Port 8080"]
    
    API -->|HTTP Requests| Controllers["Controllers/Endpoints<br/>Proxy, Weather, Workflows"]
    
    Controllers -->|Dapr State API| StateStore["Dapr State Store<br/>HTTP: /v1.0/state<br/>Backend: Redis"]
    
    Controllers -->|Dapr Pub/Sub| PubSub["Dapr Pub/Sub<br/>HTTP: /v1.0/publish<br/>Backend: Redis"]
    
    Controllers -->|Observability| DaprLogs["Dapr Sidecar Logs<br/>HTTP: 3500<br/>gRPC: 50001"]
    
    StateStore -->|Read/Write| Redis["Redis Instance<br/>Host: {env}-redis.redis.cache.windows.net<br/>Port: 6379 + TLS"]
    
    PubSub -->|Pub/Sub| Redis
    
    DaprLogs -->|Health Check| DaprHealth["Dapr Health<br/>Readiness: /healthz<br/>Liveness: /healthz"]
    
    API -->|Application Logs| LogAgent["Log Agent<br/>Container Apps Sidecar"]
    LogAgent -->|Stream Logs| LogAnalytics["Log Analytics<br/>mfe-portal-logs"]
    LogAnalytics -->|Query| KQL["KQL Queries<br/>Monitor Performance"]
    
    style API fill:#e3f2fd
    style Controllers fill:#f3e5f5
    style StateStore fill:#c8e6c9
    style PubSub fill:#fff9c4
    style Redis fill:#ffccbc
    style LogAnalytics fill:#f0f4c3
```

## Bicep Template Hierarchy

Infrastructure-as-Code deployment structure:

```mermaid
graph TD
    Main["main.bicep<br/>---<br/>Entry Point<br/>Parameter passthrough"]
    
    Main -->|Creates| ManagedID["User-Assigned Identity<br/>id-{token}<br/>Reusable across services"]
    
    Main -->|Calls| Infra["infrastructure.bicep<br/>---<br/>Foundational Resources"]
    Main -->|Calls| Container["container-app.bicep<br/>---<br/>Service Deployment"]
    
    Infra -->|Creates| LogA["Log Analytics Workspace<br/>workspace.json"]
    Infra -->|Creates| ACRRes["Container Registry<br/>Basic SKU<br/>Admin user enabled"]
    Infra -->|Creates| CAERes["Container Apps Environment<br/>Zone redundant: false"]
    Infra -->|Creates| RedisRes["Azure Cache for Redis<br/>TLS 1.2 minimum<br/>LRU policy"]
    Infra -->|Creates| KeyVaultRes["Azure Key Vault<br/>Standard SKU<br/>RBAC enabled"]
    
    KeyVaultRes -->|Stores| RedisSecret["Secret: RedisConnectionString<br/>Full connection string<br/>with auth"]
    
    Container -->|Receives| ManagedIDParam["Managed Identity ID<br/>Passed as parameter<br/>from main.bicep"]
    
    Container -->|Deploys| ContainerApp["Container App: augmentservice<br/>User-assigned identity<br/>HTTPS ingress enabled<br/>Dapr enabled"]
    
    Container -->|Assigns identity| IdentityAssign["Identity Assignment<br/>Attach managed identity<br/>to Container App"]
    
    Container -->|References| KeyVault["Key Vault URI<br/>Read secrets via<br/>Managed Identity"]
    
    Container -->|Configures| SecretRef["Secret References<br/>keyVaultUrl property<br/>Identity: System"]
    
    Container -->|Sets| Env["Environment Variables<br/>ASPNETCORE_ENVIRONMENT<br/>DAPR_HTTP_ENDPOINT<br/>DAPR_GRPC_ENDPOINT<br/>REDIS_CONNECTION_STRING"]
    
    Container -->|Assigns RBAC| RoleAssign["Role Assignments<br/>Identity: Key Vault Secrets User<br/>Scope: Key Vault"]
    
    Container -->|Scaling| Scale["Scaling Configuration<br/>Min: {1|2}<br/>Max: {3|10}<br/>Concurrent threshold: 10"]
    
    style Main fill:#fff9c4
    style ManagedID fill:#c8e6c9
    style Infra fill:#c8e6c9
    style Container fill:#bbdefb
    style LogA fill:#f0f4c3
    style ACRRes fill:#f0f4c3
    style CAERes fill:#f0f4c3
    style RedisRes fill:#f0f4c3
    style KeyVaultRes fill:#c8e6c9
    style RoleAssign fill:#bbdefb
    style IdentityAssign fill:#bbdefb
```

## Dapr Component Configuration

Local and production Dapr component setup:

```mermaid
graph LR
    subgraph Local["Local Development"]
        DockerRedis["Docker Redis<br/>localhost:6379"]
        LocalComponents["dapr/components/"]
        
        LocalComponents -->|pubsub.yaml| LocalPubSub["Redis Pub/Sub<br/>Component: pubsub<br/>Backend: local Redis"]
        LocalComponents -->|state.yaml| LocalState["Redis State<br/>Component: statestore<br/>Backend: local Redis"]
        
        DaprCLI["Dapr CLI<br/>dapr run"]
        DaprCLI -->|sidecar| LocalPubSub
        DaprCLI -->|sidecar| LocalState
        LocalPubSub -->|connect| DockerRedis
        LocalState -->|connect| DockerRedis
    end
    
    subgraph Cloud["Azure Production"]
        ProdComponents["dapr/components/"]
        
        ProdComponents -->|pubsub.yaml| ProdPubSub["Redis Pub/Sub<br/>Component: pubsub<br/>Host: $(REDIS_HOST)<br/>Port: $(REDIS_PORT)<br/>Auth: $(REDIS_PASSWORD)"]
        
        ProdComponents -->|state.yaml| ProdState["Redis State<br/>Component: statestore<br/>Host: $(REDIS_HOST)<br/>Port: $(REDIS_PORT)<br/>TLS: 1.2"]
        
        AzureRedis["Azure Redis<br/>{env}-redis.redis.cache.windows.net"]
        
        ProdPubSub -->|TLS 1.2| AzureRedis
        ProdState -->|TLS 1.2| AzureRedis
    end
    
    Local -.->|Similar Config| Cloud
    
    style Local fill:#e8f5e9
    style Cloud fill:#fce4ec
```

## Infrastructure Monitoring and Observability

Logging and monitoring infrastructure setup:

```mermaid
graph TB
    subgraph Metrics["Metrics & Logs Collection"]
        ContainerApp["Container App<br/>AugmentService"]
        DaprSidecar["Dapr Sidecar<br/>HTTP: 3500<br/>gRPC: 50001"]
        
        ContainerApp -->|stdout/stderr| LogAgent["Container Log Agent"]
        DaprSidecar -->|Dapr Runtime Logs| LogAgent
    end
    
    LogAgent -->|Stream to| LogAnalytics["Log Analytics Workspace<br/>Workspace ID: mfe-portal-{env}<br/>Retention: 30 days<br/>Pricing: PerGB2018"]
    
    LogAnalytics -->|Store| KQL["KQL Tables<br/>---<br/>ContainerAppConsoleLogs<br/>ContainerAppSystemLogs<br/>DaprLogs"]
    
    KQL -->|Query| Monitor["Monitoring & Alerts<br/>---<br/>App Health<br/>Dapr Health<br/>Redis Connection<br/>Request Metrics"]
    
    Monitor -->|Display| Portal["Azure Portal<br/>Container Apps Blade<br/>Monitor Tab"]
    
    style Metrics fill:#e1f5ff
    style LogAnalytics fill:#f1f8e9
    style KQL fill:#fff9c4
    style Monitor fill:#f0f4c3
```

## Security and Identity Configuration

User-managed identity pattern for enterprise-scale architectures:

```mermaid
graph LR
    ManagedID["User-Assigned Managed Identity<br/>id-{token}<br/>Created independently<br/>Reusable across services"]
    
    ManagedID -->|Assigned to| ContainerApp["Container App<br/>AugmentService"]
    
    ManagedID -->|Access to| ACR["Container Registry<br/>Role: AcrPull<br/>Read image permissions"]
    
    ManagedID -->|Access to| KeyVault["Azure Key Vault<br/>Role: Key Vault Secrets User<br/>Read secrets only"]
    
    ManagedID -->|Access to| LogAnalytics["Log Analytics<br/>Role: Log Analytics Contributor<br/>Send logs"]
    
    KeyVault -->|Contains| RedisSecret["Redis Connection String<br/>Secret Name: RedisConnectionString<br/>Auto-rotatable"]
    
    RedisSecret -->|Used by| Redis["Redis Instance<br/>Auth: From Key Vault<br/>TLS: 1.2+ required"]
    
    ContainerApp -->|Requests| SecretRef["Secret Reference<br/>${REDIS_CONNECTION_STRING}<br/>secretRef: redis-connection-string"]
    
    SecretRef -->|Fetches from| KeyVault
    
    style ManagedID fill:#c8e6c9
    style KeyVault fill:#c8e6c9
    style ACR fill:#bbdefb
    style LogAnalytics fill:#f0f4c3
    style RedisSecret fill:#ffe0b2
```

## Why User-Managed Identity

```mermaid
graph TB
    subgraph Comparison["System-Assigned vs User-Assigned"]
        SystemAssigned["System-Assigned<br/>---<br/>Created with Container App<br/>Deleted with Container App<br/>Single service only<br/>Cannot reuse"]
        
        UserAssigned["User-Assigned<br/>---<br/>Created independently<br/>Survives Container App deletion<br/>Multiple services can use<br/>Better for multi-service architecture"]
    end
    
    subgraph YourArch["Your Architecture"]
        ManagedID["User-Assigned Identity<br/>id-{token}"]
        AugmentService["AugmentService<br/>Uses managed identity"]
        FutureService["Future: PaymentService<br/>Can reuse same identity"]
        FutureService2["Future: NotificationService<br/>Can reuse same identity"]
        
        ManagedID -->|Assigned to| AugmentService
        ManagedID -->|Available for| FutureService
        ManagedID -->|Available for| FutureService2
    end
    
    style SystemAssigned fill:#ffcdd2
    style UserAssigned fill:#c8e6c9
    style YourArch fill:#e3f2fd
```

## Secrets Management Architecture

How secrets flow through Key Vault to running service:

```mermaid
graph TB
    Deployment["Azure Deployment<br/>Bicep Template"]
    
    Deployment -->|Creates| KeyVaultRes["Azure Key Vault<br/>kv-{token}"]
    
    Deployment -->|Stores in| RedisSecret["Secret: RedisConnectionString<br/>Value: host:port?ssl=True&password=..."]
    
    Deployment -->|Creates| ContainerApp["Container App<br/>with System Identity"]
    
    Deployment -->|Grants RBAC| RoleAssign["Role Assignment<br/>Identity: Key Vault Secrets User<br/>Scope: Key Vault"]
    
    ContainerApp -->|At Runtime| RequestSecret["Container Startup<br/>Read secretRef: redis-connection-string"]
    
    RequestSecret -->|Authenticates with| ManagedID["Managed Identity<br/>No credentials needed<br/>RBAC automatic"]
    
    ManagedID -->|Queries| KeyVault["Key Vault API<br/>GET /secrets/RedisConnectionString"]
    
    KeyVault -->|Returns| SecretValue["Decrypted Secret<br/>Redis password included<br/>TLS encrypted in transit"]
    
    SecretValue -->|Injected as| EnvVar["Environment Variable<br/>REDIS_CONNECTION_STRING<br/>Available to application"]
    
    EnvVar -->|Used by| DaprRedis["Dapr Components<br/>State store connects to Redis<br/>Pub/Sub connects to Redis"]
    
    style KeyVaultRes fill:#c8e6c9
    style RedisSecret fill:#ffe0b2
    style RoleAssign fill:#bbdefb
    style ManagedID fill:#c8e6c9
    style SecretValue fill:#fff9c4
```

## Environment Comparison Matrix

Detailed environment-specific configurations:

```mermaid
graph TB
    subgraph EnvComparison["Environment Configuration Comparison"]
        subgraph Test["TEST Environment"]
            TestName["Environment: mfe-portal-test<br/>Location: eastus<br/>Resource Group: rg-mfe-test"]
            TestCompute["Compute:<br/>CPU: 0.5 vCPU<br/>Memory: 1 GiB<br/>Replicas: 1-3"]
            TestRedis["Redis:<br/>SKU: Basic<br/>Size: C0<br/>Cost: ~$10/mo"]
            TestCost["Est. Total Cost:<br/>$25-30/month"]
        end
        
        subgraph Prod["PRODUCTION Environment"]
            ProdName["Environment: mfe-portal-prod<br/>Location: eastus<br/>Resource Group: rg-mfe-prod"]
            ProdCompute["Compute:<br/>CPU: 1 vCPU<br/>Memory: 2 GiB<br/>Replicas: 2-10"]
            ProdRedis["Redis:<br/>SKU: Standard<br/>Size: C1<br/>Cost: ~$30-40/mo"]
            ProdCost["Est. Total Cost:<br/>$75-100/month"]
        end
    end
    
    style Test fill:#e8f5e9
    style Prod fill:#fce4ec
```

## Deployment Checklist

Pre-deployment and post-deployment verification steps:

```mermaid
graph TD
    PreDeploy["PRE-DEPLOYMENT"]
    
    PreDeploy -->|1. Verify| AzAuth["Azure Authentication<br/>azd auth login<br/>Validate subscription"]
    
    AzAuth -->|2. Select| Environment["Select Environment<br/>azd env select {test|prod}<br/>or azd env new"]
    
    Environment -->|3. Check| BicepValidate["Validate Bicep Templates<br/>Check for syntax errors<br/>Verify parameters.json"]
    
    BicepValidate -->|4. Review| Resources["Review Resources<br/>Resource names<br/>Locations<br/>Sizing"]
    
    Resources -->|Ready| Provision["PROVISION PHASE<br/>azd provision"]
    
    Provision -->|Creates| ProvisionOutput["✓ Log Analytics<br/>✓ Container Registry<br/>✓ Container Apps Env<br/>✓ Redis Instance"]
    
    ProvisionOutput -->|Next| Deploy["DEPLOY PHASE<br/>azd deploy"]
    
    Deploy -->|Executes| DeployOutput["✓ Build Docker Image<br/>✓ Push to ACR<br/>✓ Update Container App<br/>✓ Deploy Revision"]
    
    DeployOutput -->|Verify| PostDeploy["POST-DEPLOYMENT"]
    
    PostDeploy -->|1. Health Check| HealthCheck["Container App Health<br/>Status: Running<br/>Replicas: Ready"]
    
    HealthCheck -->|2. Endpoint Test| EndpointTest["Test Endpoints<br/>GET /api/product<br/>GET /proxy?url=..."]
    
    EndpointTest -->|3. Dapr Verify| DaprCheck["Verify Dapr<br/>HTTP: 3500 responding<br/>gRPC: 50001 responsive"]
    
    DaprCheck -->|4. Monitoring| Monitoring["Setup Monitoring<br/>azd monitor --from 30m<br/>Verify logs flowing"]
    
    Monitoring -->|Complete| Success["✓ DEPLOYMENT SUCCESSFUL<br/>Service ready for traffic"]
    
    style PreDeploy fill:#fff9c4
    style Provision fill:#c8e6c9
    style Deploy fill:#bbdefb
    style PostDeploy fill:#fff9c4
    style Success fill:#c8e6c9
```

## Related Documentation

- [DEPLOYMENT.md](DEPLOYMENT.md) - Step-by-step deployment guide
- [ARCHITECTURE.md](ARCHITECTURE.md) - Clean architecture overview
- [DAPR_SETUP.md](DAPR_SETUP.md) - Dapr local development and production setup
- [../azure.yaml](../azure.yaml) - Azure Developer CLI configuration

## Quick Reference Commands

```bash
# Environment management
azd auth login                    # Authenticate with Azure
azd env new <name>              # Create new environment
azd env select <name>           # Switch environment
azd env list                    # List all environments

# Deployment
azd provision                   # Create/update Azure resources
azd deploy                      # Build and deploy container image

# Monitoring & Cleanup
azd monitor --from 30m         # View logs from last 30 minutes
azd down                       # Delete all Azure resources
```
