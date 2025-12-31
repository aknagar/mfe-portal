# AugmentService API Documentation

## Overview
The AugmentService is a reverse proxy microservice that provides HTTP request proxying capabilities with built-in health monitoring and API documentation.

**Base URL:** `https://localhost:7139`

---

## Application APIs

Business logic endpoints that provide the core functionality of the service.

| Endpoint | Version | Description |
|----------|---------|-------------|
| `/proxy` | v1 | Forwards an HTTP GET request to an external URL and returns the response content with the original content type. Supports both text-based and binary responses. |
| `/swagger` | v1 | Interactive Swagger UI for exploring and testing APIs |
| `/openapi/v1.json` | v1 | OpenAPI 3.0.1 specification document |

---

## System APIs

Infrastructure and operational endpoints for monitoring, health checks, and service observability.

| Endpoint | Version | Description |
|----------|---------|-------------|
| `/health` | v1 | Full comprehensive health check of all registered health indicators. |
| `/alive` | v1 | Quick liveness probe for Kubernetes/container orchestrators checking only essential health indicators. |

---
