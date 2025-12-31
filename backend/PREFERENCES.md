# Project Preferences & Configuration

## Security Preferences

### HTTPS-Only Policy

**Requirement:** All services must run on HTTPS ports only. HTTP is disabled for security.

**Rationale:**
- Encrypts data in transit between client and server
- Prevents man-in-the-middle attacks
- Protects sensitive data (health checks, proxy requests)
- Industry best practice and compliance requirement

---

## Service Configuration

### AugmentService

| Setting | Current | Preferred | Status |
|---------|---------|-----------|--------|
| **HTTP Port** | 5104 | DISABLED | ❌ Needs Update |
| **HTTPS Port** | 7139 | 7139 (Enabled) | ✅ Active |
| **Protocol** | HTTP + HTTPS | HTTPS Only | ❌ Needs Update |
| **Force HTTPS Redirect** | Disabled | Enabled | ❌ Needs Update |

### Implementation Plan

1. **Remove HTTP from launchSettings.json**
   - Remove `http://localhost:5104` from `applicationUrl`
   - Keep only `https://localhost:7139`

2. **Enable HTTPS Redirect in Program.cs**
   - Ensure `app.UseHttpsRedirection()` is active
   - This will redirect any HTTP requests to HTTPS

3. **Test HTTPS Endpoints**
   - All requests should use `https://localhost:7139`
   - HTTP requests should be redirected to HTTPS

---

## Affected Endpoints

### AugmentService - HTTPS Only URLs

| Endpoint | URL | Type |
|----------|-----|------|
| **Proxy** | `https://localhost:7139/proxy?url=...` | Application API |
| **Health Check** | `https://localhost:7139/health` | System API |
| **Liveness Probe** | `https://localhost:7139/alive` | System API |
| **Swagger UI** | `https://localhost:7139/swagger` | Documentation |
| **OpenAPI Spec** | `https://localhost:7139/openapi/v1.json` | Documentation |

---

## Configuration Files to Update

### 1. AugmentService/Properties/launchSettings.json
- Change `applicationUrl` from `"https://localhost:7139;http://localhost:5104"` to `"https://localhost:7139"`
- Remove HTTP profile or disable it

### 2. AugmentService/Program.cs
- Verify `app.UseHttpsRedirection()` is enabled
- This is already implemented and active

### 3. Any Client Code
- Update all API calls to use HTTPS URLs
- Update documentation with HTTPS-only URLs

---

## Notes

- Self-signed certificate is used in development (localhost)
- Clients may need to disable certificate validation in development environments
- PowerShell example with certificate validation disabled:
  ```powershell
  $PSDefaultParameterValues["Invoke-WebRequest:SkipCertificateCheck"] = $true
  Invoke-WebRequest -Uri "https://localhost:7139/health"
  ```

---

## Future Considerations

- [ ] Update all API clients to use HTTPS URLs
- [ ] Document certificate setup for production deployment
- [ ] Configure certificate pinning if needed
- [ ] Set up HSTS (HTTP Strict Transport Security) headers
- [ ] Consider mutual TLS (mTLS) for service-to-service communication
