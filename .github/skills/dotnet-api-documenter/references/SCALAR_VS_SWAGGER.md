# Scalar UI vs Swagger UI Comparison

## Feature Matrix

| Feature | Scalar UI | Swagger UI |
|---------|-----------|-----------|
| **UI/UX** | Modern, intuitive | Classic, familiar |
| **Dark Mode** | Built-in | Third-party themes |
| **Performance** | Lightweight | Standard |
| **Read-Only Mode** | ✓ | ✗ |
| **Mock Server** | ✓ | ✗ |
| **Request/Response Samples** | ✓ | ✓ |
| **Try-It-Out** | ✓ | ✓ |
| **Authentication** | ✓ | ✓ |
| **OpenAPI Support** | 3.0, 3.1 | 3.0, 3.1 |
| **Community** | Growing | Very Large |
| **Enterprise Support** | ✓ | ✓ |

## When to Use Scalar UI

- Internal APIs with modern development teams
- APIs that need a sleek, modern presentation
- Projects where UI/UX is a priority
- Read-only public documentation
- Mock/testing scenarios

## When to Use Swagger UI

- Enterprise/legacy systems expecting standard tools
- Public APIs where standardization matters
- Need maximum ecosystem tool support
- Team familiarity with Swagger ecosystem
- Integrations with existing Swagger tooling

## Installation Comparison

### Scalar UI
```xml
<PackageReference Include="Scalar.AspNetCore" Version="1.2.28" />
```
```csharp
app.MapScalarApiReference();
```
Accessible at: `/scalar`

### Swagger UI
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
```
```csharp
app.UseSwaggerUI();
```
Accessible at: `/swagger`

## Configuration Examples

### Scalar - Branded & Read-Only
```csharp
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("My API Documentation")
        .WithDarkMode(true)
        .WithDefaultHttpClient()
        .WithOpenApiRoutePattern("/openapi/{documentName}.json");
});
```

### Swagger - Custom Styling
```csharp
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "api-docs";
    c.DefaultModelsExpandDepth(0);
    c.DocExpansion(DocExpansion.List);
});
```

## Mock Server Feature (Scalar Only)

Scalar UI includes built-in mock server:
- Automatically generates mock responses
- Test API without actual backend
- Useful for contract-first development
- Speeds up frontend development

```csharp
// Scalar automatically generates mocks from your OpenAPI spec
// No additional configuration needed
app.MapScalarApiReference(options =>
{
    options.WithDefaultHttpClient();
});
```

## URL Patterns

**Swagger UI:**
- Docs: `/swagger`
- Spec: `/swagger/v1/swagger.json`
- Multiple versions: `/swagger/v1`, `/swagger/v2`

**Scalar UI:**
- Docs: `/scalar`
- Spec: `/openapi/v1.json`
- Can customize route pattern

## Export & Integration

Both support:
- OpenAPI JSON export
- OpenAPI YAML export
- Client code generation
- Integration with API gateways
- CI/CD pipelines

### Generate Clients from OpenAPI

```powershell
# Install OpenAPI Generator
dotnet tool install -g openapi-generator-cli

# Generate C# client
openapi-generator-cli generate `
  -i https://api.example.com/swagger/v1/swagger.json `
  -g csharp-dotnet `
  -o ./GeneratedClient

# Generate TypeScript client
openapi-generator-cli generate `
  -i https://api.example.com/swagger/v1/swagger.json `
  -g typescript-fetch `
  -o ./GeneratedClient
```

## Recommendation for MFE Portal

For `AugmentService`:
- **Use Scalar UI** if modernizing UI/UX is priority
- **Use Swagger UI** if standardization across enterprise is important
- **Use Both** for flexibility (expose both endpoints)

Both can coexist in the same application:
```csharp
app.UseSwagger();
app.UseSwaggerUI(c => c.RoutePrefix = "swagger");
app.MapScalarApiReference(options => options.WithOpenApiRoutePattern("/openapi/{documentName}.json"));

// Access both:
// Scalar: /scalar
// Swagger: /swagger
```
