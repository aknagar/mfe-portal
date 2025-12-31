# AugmentService API Documentation

## Overview
The AugmentService is a reverse proxy microservice that provides HTTP request proxying capabilities with built-in health monitoring and API documentation.

**Base URL:** `https://localhost:7139`

---

## Application APIs

Business logic endpoints that provide the core functionality of the service.

### Product Management
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/Product` | GET | Retrieve all products from the database |
| `/api/Product/{id}` | GET | Retrieve a specific product by ID |
| `/api/Product` | POST | Create a new product |
| `/api/Product/{id}` | PUT | Update an existing product |
| `/api/Product/{id}` | DELETE | Delete a product by ID |

### Weather Data
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/weather/{date}` | GET | Retrieve weather data for a specific date (User access) |
| `/admin/weather/{date}` | DELETE | Delete weather data for a specific date (Admin access) |

### HTTP Proxy
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/proxy` | GET | Forward HTTP GET requests to external URLs with streaming response support. Query parameter: `url` (target URL) |

### Documentation & API
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/scalar/v1` | GET | Interactive Scalar UI for exploring and testing APIs (Modern OpenAPI documentation) |
| `/openapi/v1.json` | GET | OpenAPI 3.0.1 specification document |

### Service Integration
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/notify` | POST | Send notification messages to Azure Service Bus topic |

---

## System APIs

Infrastructure and operational endpoints for monitoring, health checks, and service observability.

| Endpoint | Version | Description |
|----------|---------|-------------|
| `/health` | v1 | Full comprehensive health check of all registered health indicators. |
| `/alive` | v1 | Quick liveness probe for Kubernetes/container orchestrators checking only essential health indicators. |

---

## API Documentation Access

- **Scalar UI** (Modern): `https://localhost:7139/scalar/v1`
- **OpenAPI JSON**: `https://localhost:7139/openapi/v1.json`
