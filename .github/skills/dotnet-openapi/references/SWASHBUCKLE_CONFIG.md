# Swashbuckle Advanced Configuration

## Custom Operation Filters

Filter to add authorization header to all operations:

```csharp
public class AddAuthorizationHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authAttribute = context.MethodInfo.DeclaringType?
            .GetCustomAttribute<AuthorizeAttribute>();

        if (authAttribute != null)
        {
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                }
            };
        }
    }
}

// Register in Program.cs
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<AddAuthorizationHeaderOperationFilter>();
});
```

## Schema Filters

Exclude properties from schema:

```csharp
public class ExcludePropertiesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null)
            return;

        var propertiesToRemove = schema.Properties
            .Where(p => p.Key.StartsWith("Internal"))
            .Select(p => p.Key)
            .ToList();

        foreach (var prop in propertiesToRemove)
        {
            schema.Properties.Remove(prop);
        }
    }
}

// Register
builder.Services.AddSwaggerGen(c =>
{
    c.SchemaFilter<ExcludePropertiesSchemaFilter>();
});
```

## Document Request/Response Bodies

```csharp
/// <summary>
/// Processes a batch of items
/// </summary>
/// <param name="request">Batch request containing items to process</param>
/// <returns>Processing result with summary</returns>
[HttpPost("process-batch")]
[ProducesResponseType(typeof(BatchResultDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<BatchResultDto>> ProcessBatch([FromBody] BatchRequestDto request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var result = await _service.ProcessAsync(request);
    return Ok(result);
}
```

## Content Type Negotiation

Specify supported content types:

```csharp
[HttpPost]
[Consumes("application/json")]
[Produces("application/json")]
public ActionResult<ItemDto> CreateItem([FromBody] CreateItemDto dto)
{
}

[HttpGet]
[Produces("application/json", "application/xml")]
public ActionResult<ItemDto> GetItem(int id)
{
}
```

## Multiple Versions

Document multiple API versions:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "Version 1 - Legacy"
    });

    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "My API",
        Version = "v2",
        Description = "Version 2 - Current"
    });
});

// In Startup configuration
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "API v2");
});
```

## Exclude Endpoints

```csharp
// Don't include in API documentation
[ApiExplorerSettings(IgnoreApi = true)]
[HttpGet("health")]
public ActionResult Health() => Ok();

// Include but as deprecated
[Obsolete("Use GetUserV2 instead")]
[HttpGet("user/{id}")]
public ActionResult<UserDto> GetUser(int id) => Ok();
```

## OpenAPI Spec Options

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "Complete API documentation",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Support",
            Email = "support@example.com",
            Url = new Uri("https://example.com/support")
        },
        License = new OpenApiLicense
        {
            Name = "Apache 2.0",
            Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
        }
    });
});
```

## Rename/Reorder Endpoints

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<RenameOperationFilter>();
});

public class RenameOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Rename operation summary for clarity
        if (context.ApiDescription.HttpMethod == "POST" 
            && context.ApiDescription.ActionDescriptor.RouteValues["action"] == "Create")
        {
            operation.OperationId = "CreateNewEntity";
            operation.Summary = "Create a new entity";
        }
    }
}
```
