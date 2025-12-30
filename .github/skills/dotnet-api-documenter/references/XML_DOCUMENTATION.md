# XML Documentation Best Practices

## Summary Tags

```csharp
/// <summary>
/// Calculates the total price including tax and shipping
/// </summary>
public decimal CalculateTotal() { }
```

## Parameter Documentation

```csharp
/// <summary>
/// Creates a new order
/// </summary>
/// <param name="customerId">The unique customer identifier</param>
/// <param name="items">Collection of line items to order</param>
/// <param name="expedited">Whether to use expedited shipping</param>
public Order CreateOrder(int customerId, List<OrderItem> items, bool expedited = false) { }
```

## Return Value Documentation

```csharp
/// <summary>
/// Retrieves order details
/// </summary>
/// <returns>Order object containing all order information, null if not found</returns>
public Order GetOrder(int id) { }

/// <summary>
/// Validates the order
/// </summary>
/// <returns>True if valid, false otherwise</returns>
public bool ValidateOrder(Order order) { }
```

## Exception Documentation

```csharp
/// <summary>
/// Processes payment
/// </summary>
/// <exception cref="ArgumentNullException">Thrown when payment is null</exception>
/// <exception cref="InvalidOperationException">Thrown when amount is negative</exception>
/// <exception cref="PaymentProcessingException">Thrown if payment gateway fails</exception>
public async Task<PaymentResult> ProcessPaymentAsync(Payment payment) { }
```

## Response Types for OpenAPI

Use OpenAPI response type attributes to document all possible responses:

```csharp
/// <summary>
/// Gets an order
/// </summary>
/// <param name="id">Order ID</param>
/// <returns>Order data</returns>
/// <response code="200">Order found</response>
/// <response code="404">Order not found</response>
/// <response code="500">Server error</response>
[HttpGet("{id}")]
[ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public ActionResult<OrderDto> GetOrder(int id) { }
```

## Remarks for Additional Context

```csharp
/// <summary>
/// Sends an email notification
/// </summary>
/// <remarks>
/// This method queues the email for asynchronous delivery.
/// Actual delivery typically occurs within 5 minutes.
/// 
/// The email template is selected based on the notification type.
/// Requires SMTP configuration to be present.
/// </remarks>
public Task SendNotificationAsync(Notification notification) { }
```

## Example Usage

```csharp
/// <summary>
/// Creates a new product
/// </summary>
/// <param name="createDto">Product details</param>
/// <returns>Created product with ID</returns>
/// <example>
/// <code>
/// var dto = new CreateProductDto 
/// { 
///   Name = "Widget", 
///   Price = 29.99m 
/// };
/// var product = await productService.CreateAsync(dto);
/// </code>
/// </example>
[HttpPost]
[ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createDto) { }
```

## Deprecated Methods

```csharp
/// <summary>
/// Gets user by ID
/// </summary>
/// <param name="id">User ID</param>
/// <returns>User object</returns>
/// <remarks>
/// <deprecated version="2.0">Use GetUserV2 instead. This method will be removed in version 3.0.</deprecated>
/// </remarks>
[Obsolete("Use GetUserV2 instead", false)]
public UserDto GetUser(int id) { }
```

## Inheritance Documentation

```csharp
/// <summary>
/// Base service for all data operations
/// </summary>
/// <remarks>
/// Derived classes should override ValidateEntity to provide entity-specific validation.
/// </remarks>
public abstract class BaseService<T> where T : class
{
    /// <summary>
    /// Validates entity
    /// </summary>
    /// <param name="entity">Entity to validate</param>
    /// <returns>Validation result</returns>
    /// <remarks>Derived classes should provide their own implementations</remarks>
    protected virtual ValidationResult ValidateEntity(T entity) => new();
}
```

## Cross-Reference Documentation

```csharp
/// <summary>
/// Sends message using MessageQueue
/// </summary>
/// <param name="message">Message to send</param>
/// <remarks>
/// This method uses <see cref="MessageQueue"/> for delivery.
/// See also: <see cref="ProcessMessageAsync"/>
/// </remarks>
/// <seealso cref="MessageQueue"/>
/// <seealso cref="ProcessMessageAsync"/>
public Task SendMessageAsync(Message message) { }
```

## Controller Documentation

```csharp
/// <summary>
/// Manages customer operations
/// </summary>
/// <remarks>
/// All endpoints in this controller require authentication.
/// Customer ID must be owned by the authenticated user.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    // Endpoints...
}
```

## DTO Documentation

```csharp
/// <summary>
/// Request to create a new customer
/// </summary>
public class CreateCustomerDto
{
    /// <summary>
    /// Customer's first name
    /// </summary>
    /// <remarks>Required. Maximum 100 characters.</remarks>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; }

    /// <summary>
    /// Customer's email address
    /// </summary>
    /// <remarks>Must be a valid email format</remarks>
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// Customer's age
    /// </summary>
    /// <remarks>Optional. Must be between 18 and 120 if provided.</remarks>
    [Range(18, 120)]
    public int? Age { get; set; }
}
```

## Configuration in .csproj

```xml
<PropertyGroup>
    <!-- Enable XML documentation file generation -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    
    <!-- Specify output location -->
    <DocumentationFile>bin\$(Configuration)\$(TargetFramework)\$(AssemblyName).xml</DocumentationFile>
    
    <!-- Suppress warning about missing documentation (optional) -->
    <NoWarn>$(NoWarn);1591</NoWarn>
    
    <!-- Treat documentation warnings as errors (optional) -->
    <!-- <TreatWarningsAsErrors>true</TreatWarningsAsErrors> -->
</PropertyGroup>
```

## Enable XML Comments in Swagger

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "My API", 
        Version = "v1" 
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});
```
