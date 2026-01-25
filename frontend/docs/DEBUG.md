# Debug Guide

## Build Frontend

### Docker Image Build
Build the frontend Docker image for use with Aspire:

```powershell
cd frontend
docker build -t frontend:latest .
```

## Run Frontend Aspire


### Start Aspire with Frontend
Run the frontend Aspire which includes the frontend container:

```powershell
cd frontend
aspire run
```

**Aspire Dashboard**: https://localhost:15002

### Access Frontend
Once Aspire starts, the frontend will be available at:
- **Frontend Application**: http://localhost:1234
