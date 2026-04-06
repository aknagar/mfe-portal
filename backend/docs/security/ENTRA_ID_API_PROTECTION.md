# Protecting the Web API with Azure Entra ID

This document explains how `AugmentService.Api` is protected with Bearer-token authentication in production, and walks through every step needed in Azure Entra ID (formerly Azure AD) to obtain the three values the app requires: **TenantId**, **ClientId**, and **Audience**.

---

## Table of Contents

1. [How authentication works in this project](#1-how-authentication-works-in-this-project)
2. [Concepts you need to know](#2-concepts-you-need-to-know)
3. [Step-by-step: Register the API in Entra ID](#3-step-by-step-register-the-api-in-entra-id)
4. [Step-by-step: Register a client application](#4-step-by-step-register-a-client-application)
5. [Putting it all together in appsettings.json](#5-putting-it-all-together-in-appsettingsjson)
6. [Verifying it works](#6-verifying-it-works)
7. [How to protect individual endpoints](#7-how-to-protect-individual-endpoints)
8. [Granting API permissions to a user or service principal](#8-granting-api-permissions-to-a-user-or-service-principal)
9. [Environment-specific configuration](#9-environment-specific-configuration)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. How authentication works in this project

`Program.cs` branches on the runtime environment:

```
Development / Test  ──► Lenient JWT scheme
                         • Any token is accepted (or no token — a fake "Dev User" is injected)
                         • Lets you call the API from Scalar UI / integration tests without Entra ID

Production           ──► Microsoft.Identity.Web
                         • Every request must carry a valid Bearer token issued by Entra ID
                         • Token is validated against your tenant's signing keys automatically
                         • Roles and scopes in the token become claims on HttpContext.User
```

The relevant code in `Program.cs`:

```csharp
if (builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Test")
{
    // Lenient dev/test JWT setup (no real validation)
    builder.Services.AddAuthentication(...)
        .AddJwtBearer(options => { ... });
}
else
{
    // Production: validates tokens against Azure Entra ID
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(
        builder.Configuration, "AzureAd");
}
```

`AddMicrosoftIdentityWebApiAuthentication` reads the `AzureAd` section of configuration:

```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "TenantId":  "<directory (tenant) ID>",
  "ClientId":  "<application (client) ID>",
  "Audience":  "api://<application (client) ID>"
}
```

---

## 2. Concepts you need to know

| Term | Meaning |
|------|---------|
| **Tenant** | Your Azure AD / Entra ID directory. Every organisation has exactly one. |
| **TenantId** | The GUID that uniquely identifies your tenant. Found in Entra ID → Overview. |
| **App Registration** | A record in Entra ID that represents one application (your API, a SPA, a daemon, etc.). |
| **ClientId** | The GUID assigned to an App Registration. Also called "Application ID". |
| **Audience** | The value the API expects in the `aud` claim of incoming tokens. Conventionally `api://<clientId>`. |
| **Scope** | A permission string that callers request (`api://<clientId>/Weather.Read`). |
| **Bearer token** | A short-lived JWT. The caller obtains one from Entra ID and sends it in the `Authorization: Bearer <token>` header. |

---

## 3. Step-by-step: Register the API in Entra ID

This registration represents **your backend API** — not the human user or the frontend.

### 3.1 Create the App Registration

1. Open the [Azure Portal](https://portal.azure.com) and navigate to  
   **Azure Active Directory** (or search "Entra ID") → **App registrations** → **New registration**.

2. Fill in:
   | Field | Value |
   |-------|-------|
   | **Name** | `mfe-portal-api` (or any descriptive name) |
   | **Supported account types** | *Accounts in this organizational directory only* (single-tenant) |
   | **Redirect URI** | Leave blank — APIs don't need a redirect URI |

3. Click **Register**.

4. On the **Overview** page of the new registration, copy:
   - **Application (client) ID** → this is your `ClientId`
   - **Directory (tenant) ID** → this is your `TenantId`

### 3.2 Expose the API (define the Audience and scopes)

1. In the left menu, click **Expose an API**.

2. Next to **Application ID URI**, click **Set** (or **Add**).  
   Accept the default value `api://<clientId>` and click **Save**.  
   This URI becomes the **Audience** your API validates against.

3. Click **Add a scope** to define permissions callers can request:

   | Field | Example value |
   |-------|---------------|
   | Scope name | `Weather.Read` |
   | Who can consent | Admins and users |
   | Admin consent display name | Read weather data |
   | Admin consent description | Allows the app to read weather data on behalf of the signed-in user. |
   | State | Enabled |

   Repeat for any other scopes your API exposes (e.g. `Orders.Write`, `Products.Read`).

4. Your full scope string will look like:  
   `api://<clientId>/Weather.Read`

### 3.3 (Optional) Define App Roles

App Roles let you perform role-based authorization (`[Authorize(Roles = "Admin")]`).

1. In the left menu, click **App roles** → **Create app role**.
2. Fill in:
   | Field | Example |
   |-------|---------|
   | Display name | `Admin` |
   | Allowed member types | Users/Groups |
   | Value | `Admin` |
   | Description | Full administrative access |
3. Click **Apply**.

---

## 4. Step-by-step: Register a client application

Any application that needs to **call** the API must have its own App Registration.  
Repeat this section for each caller: the frontend SPA, a Postman client, a daemon service, etc.

### 4.1 Create the client App Registration

1. **App registrations** → **New registration**.

2. Fill in:
   | Field | Value |
   |-------|-------|
   | **Name** | `mfe-portal-frontend` (or `mfe-portal-postman`, etc.) |
   | **Supported account types** | Same as the API registration |
   | **Redirect URI** | For a SPA: `http://localhost:1234` (dev) and your production URL |

3. Click **Register**.

### 4.2 Grant the client permission to call the API

1. In the client registration, go to **API permissions** → **Add a permission**.
2. Choose **My APIs** → select `mfe-portal-api`.
3. Select **Delegated permissions** → tick the scopes you want (e.g. `Weather.Read`).
4. Click **Add permissions**.
5. If your scopes require admin consent, click **Grant admin consent for \<tenant\>**.

### 4.3 Create a client secret (for confidential clients / daemons)

Skip this step for SPAs — they use PKCE instead.

1. In the client registration → **Certificates & secrets** → **New client secret**.
2. Give it a description and expiry, then click **Add**.
3. **Copy the secret value immediately** — you cannot retrieve it again.
4. Store it in Key Vault or GitHub Secrets (never in `appsettings.json`).

---

## 5. Putting it all together in appsettings.json

Open `backend/AugmentService/AugmentService.Api/appsettings.json` and fill in the values from Step 3.1 and 3.2:

```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "TenantId":  "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "ClientId":  "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy",
  "Audience":  "api://yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy"
}
```

> **Security note:** `TenantId` and `ClientId` are **not secrets** — they are publicly visible in tokens. Do not commit `ClientSecret` here. Use Key Vault or environment variables for any secret material.

### Where to find each value

| Config key | Where to find it |
|------------|-----------------|
| `Instance` | Always `https://login.microsoftonline.com/` for standard tenants |
| `TenantId` | Entra ID → **Overview** → Directory (tenant) ID |
| `ClientId` | Your API's App Registration → **Overview** → Application (client) ID |
| `Audience` | Your API's App Registration → **Expose an API** → Application ID URI |

---

## 6. Verifying it works

### 6.1 Get a token using the Azure CLI

```bash
# Log in first
az login

# Request a token for your API audience
az account get-access-token --resource "api://<clientId>" --query accessToken -o tsv
```

### 6.2 Call the API with the token

```bash
TOKEN=$(az account get-access-token --resource "api://<clientId>" --query accessToken -o tsv)

curl -H "Authorization: Bearer $TOKEN" https://<your-api-url>/weatherforecast
```

### 6.3 Inspect the token

Paste the token at [jwt.ms](https://jwt.ms) to inspect claims:

| Claim | Meaning |
|-------|---------|
| `aud` | Must match your `Audience` config value |
| `iss` | `https://login.microsoftonline.com/<tenantId>/v2.0` |
| `oid` | Object ID of the caller |
| `scp` | Scopes granted (delegated flow) |
| `roles` | App roles assigned to the caller |
| `exp` | Token expiry (Unix timestamp) |

### 6.4 Common validation failures

| HTTP status | Likely cause |
|-------------|-------------|
| `401 Unauthorized` | No `Authorization` header, or token is expired / malformed |
| `401` with `WWW-Authenticate: Bearer error="invalid_token"` | `aud` claim does not match `Audience` config |
| `401` with `invalid_token, The issuer is invalid` | `TenantId` is wrong or token came from a different tenant |
| `403 Forbidden` | Token is valid but caller lacks the required role or scope |

---

## 7. How to protect individual endpoints

### Require authentication on a controller

```csharp
[ApiController]
[Authorize]               // Requires any valid token
[Route("[controller]")]
public class WeatherForecastController : ControllerBase { ... }
```

### Require a specific scope

```csharp
[Authorize]
[RequiredScope("Weather.Read")]   // from Microsoft.Identity.Web
public IActionResult GetForecast() { ... }
```

### Require a specific App Role

```csharp
[Authorize(Roles = "Admin")]
public IActionResult DeleteAllOrders() { ... }
```

### Allow anonymous access to one endpoint within a protected controller

```csharp
[AllowAnonymous]
[HttpGet("health")]
public IActionResult Health() => Ok("healthy");
```

### Accessing token claims in code

```csharp
// Object ID of the authenticated user
var userId = User.GetObjectId();            // extension from Microsoft.Identity.Web

// Email / UPN
var email = User.FindFirstValue(ClaimTypes.Email)
             ?? User.FindFirstValue("preferred_username");
```

---

## 8. Granting API permissions to a user or service principal

### For a human user

1. Entra ID → **Enterprise applications** → find your API app (`mfe-portal-api`).
2. **Users and groups** → **Add user/group**.
3. Assign the user to the required App Role (e.g. `Admin`).

### For a service principal (another app / daemon)

1. In the **client** App Registration → **API permissions** → add the required scopes/roles from `mfe-portal-api`.
2. Click **Grant admin consent**.
3. The service principal will receive the role in the `roles` claim of its token.

---

## 9. Environment-specific configuration

| Environment | How AzureAd values are supplied |
|-------------|--------------------------------|
| **Local Development** | Not used — Dev JWT scheme bypasses Entra ID entirely |
| **Test** | Not used — same bypass as development |
| **Staging / Production** | `appsettings.json` base values + override via environment variables or Key Vault |

### Overriding with environment variables (recommended for containers)

ASP.NET Core maps double-underscore (`__`) to config section separators.  
In your container / App Service, set:

```
AzureAd__TenantId  = xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
AzureAd__ClientId  = yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy
AzureAd__Audience  = api://yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy
```

In the Aspire AppHost you can inject these into the Container App:

```csharp
augmentService
    .WithEnvironment("AzureAd__TenantId", tenantId)
    .WithEnvironment("AzureAd__ClientId", clientId)
    .WithEnvironment("AzureAd__Audience", $"api://{clientId}");
```

---

## 10. Troubleshooting

### "AADSTS700016: Application not found in the directory"

The `ClientId` in your config does not exist in the tenant identified by `TenantId`.  
Double-check both values in the Azure Portal.

### "AADSTS50194: Application is not configured as a multi-tenant application"

Your App Registration is set to single-tenant but the token was issued by a different tenant.  
Either change **Supported account types** to *Multitenant* or ensure callers authenticate against your own tenant.

### "IDX10214: Audience validation failed"

The `aud` claim in the token does not match your `Audience` config value.  
Ensure the caller is requesting a token with `scope = api://<clientId>/.default` (not the Microsoft Graph audience).

### Token works in jwt.ms but API still returns 401

Check that `UseAuthentication()` is called **before** `UseAuthorization()` in `Program.cs` (it is — but double-check if the pipeline was changed):

```csharp
app.UseAuthentication();   // must be first
app.UseAuthorization();
```

### API returns 401 only in production, not in development

Expected — the development environment uses a bypass scheme. Check that `ASPNETCORE_ENVIRONMENT` is set to `Production` in the container and that the `AzureAd` config section is fully populated.

---

## References

- [Microsoft.Identity.Web — quickstart for Web APIs](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-configure-app-expose-web-apis)
- [Register an application with Entra ID](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [Expose an API — scopes and app roles](https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-add-app-roles-in-apps)
- [Protected web API — code configuration](https://learn.microsoft.com/en-us/azure/active-directory/develop/scenario-protected-web-api-app-configuration)
- [jwt.ms — inspect and decode tokens](https://jwt.ms)
- [Azure CLI: get-access-token](https://learn.microsoft.com/en-us/cli/azure/account#az-account-get-access-token)
