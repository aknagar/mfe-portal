# Frontend Container Setup

This guide explains how to build and publish the MFE Portal frontend container.

## Prerequisites

- Docker Desktop installed and running
- .NET Aspire CLI installed (`dotnet tool install -g aspire`)
- Azure CLI authenticated (for deployment)

## Building the Container Locally

### Build the Docker image

```bash
cd frontend
docker build -t mfe-portal-frontend .
```

### Build without cache (fresh build)

```bash
docker build --no-cache -t mfe-portal-frontend .
```

### Build with version metadata

```bash
docker build \
  --build-arg VERSION=1.0.0 \
  --build-arg BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ") \
  --build-arg VCS_REF=$(git rev-parse --short HEAD) \
  -t mfe-portal-frontend:1.0.0 \
  .
```

## Running the Container Locally

### Run the container

```bash
docker run -p 1234:1234 mfe-portal-frontend
```

### Run with environment variables

```bash
docker run -p 1234:1234 \
  -e NODE_ENV=production \
  mfe-portal-frontend
```

Access the application at: http://localhost:1234

## Publishing to Azure Container Apps

### Deploy using Aspire

The frontend uses .NET Aspire for deployment orchestration:

```bash
cd frontend
aspire deploy
```

This will:
1. Build the Docker image from the Dockerfile
2. Authenticate with Azure
3. Push the image to Azure Container Registry
4. Deploy to Azure Container Apps
5. Configure the container with port 80 (Azure requirement)

### View deployment details with debug logging

```bash
aspire deploy --log-level debug
```

### Run Aspire locally (development)

```bash
aspire run apphost.cs
```

This starts the Aspire dashboard at https://localhost:17147

## Container Configuration

### Ports

- **Development**: Port 1234 (serve default)
- **Azure Container Apps**: Port 80 (external) → Port 1234 (internal)

### Build Stages

The Dockerfile uses multi-stage builds:

1. **Builder stage** (`node:20-alpine`): Installs dependencies and builds the application
2. **Production stage** (`node:20-alpine`): Runs the static file server with built assets

### Image Size Optimization

- Uses Alpine Linux base image (~5MB)
- Multi-stage build (no build tools in final image)
- Only production dependencies included
- Built assets served via lightweight `serve` package

## Deployment Endpoints

After successful deployment, the application is available at:

- **Application**: `https://frontend.bravemeadow-7c866cd0.centralindia.azurecontainerapps.io`
- **Aspire Dashboard**: `https://aspire-dashboard.ext.bravemeadow-7c866cd0.centralindia.azurecontainerapps.io`

## Troubleshooting

### npm authentication errors during build

If you see `npm error code E401`, ensure the Dockerfile uses:
```dockerfile
RUN npm install --ignore-scripts --legacy-peer-deps
```

### Port conflicts

If port 1234 is in use locally:
```bash
docker run -p 3000:1234 mfe-portal-frontend
```

### Container not starting

Check logs:
```bash
docker logs <container-id>
```

View running containers:
```bash
docker ps
```

## Files Reference

- **Dockerfile**: Multi-stage build configuration
- **apphost.cs**: Aspire deployment configuration
- **apphost.run.json**: Aspire runtime settings
- **.dockerignore**: Files excluded from Docker build context
