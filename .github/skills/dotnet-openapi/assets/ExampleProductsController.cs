<!-- Example Controller with Complete OpenAPI Documentation -->
<!-- Shows best practices for documenting endpoints -->

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyApi.Controllers;

/// <summary>
/// API endpoints for managing products
/// </summary>
/// <remarks>
/// All endpoints in this controller require authentication.
/// Products are scoped to the authenticated user's organization.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of products
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="search">Optional search filter by product name</param>
    /// <returns>Paginated list of products</returns>
    /// <response code="200">List of products retrieved successfully</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="500">Server error occurred</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResultDto<ProductDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        // Implementation
        return Ok(new PagedResultDto<ProductDto> { Items = new List<ProductDto>(), Total = 0 });
    }

    /// <summary>
    /// Retrieves a single product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    /// <response code="200">Product found</response>
    /// <response code="404">Product not found</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        // Implementation
        return Ok(new ProductDto { Id = id, Name = "Sample Product" });
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="dto">Product data to create</param>
    /// <returns>Created product with generated ID</returns>
    /// <response code="201">Product created successfully</response>
    /// <response code="400">Validation failed or invalid input</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="409">Product with same name already exists</response>
    /// <example>
    /// <code>
    /// POST /api/products
    /// {
    ///   "name": "Widget",
    ///   "description": "A useful widget",
    ///   "price": 29.99,
    ///   "sku": "WIDGET-001"
    /// }
    /// </code>
    /// </example>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto dto)
    {
        // Implementation
        return CreatedAtAction(nameof(GetProduct), new { id = 1 }, new ProductDto { Id = 1, Name = dto.Name });
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="id">Product ID to update</param>
    /// <param name="dto">Updated product data</param>
    /// <returns>Updated product</returns>
    /// <response code="200">Product updated successfully</response>
    /// <response code="400">Validation failed or invalid input</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">Product not found</response>
    /// <response code="409">Conflict: Another user modified the product</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
    {
        // Implementation
        return Ok(new ProductDto { Id = id, Name = dto.Name });
    }

    /// <summary>
    /// Deletes a product
    /// </summary>
    /// <param name="id">Product ID to delete</param>
    /// <response code="204">Product deleted successfully</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">Product not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        // Implementation
        return NoContent();
    }
}

/// <summary>
/// DTO for product response
/// </summary>
public class ProductDto
{
    /// <summary>Product unique identifier</summary>
    public int Id { get; set; }

    /// <summary>Product name</summary>
    public string Name { get; set; }

    /// <summary>Product description</summary>
    public string? Description { get; set; }

    /// <summary>Product price</summary>
    public decimal Price { get; set; }

    /// <summary>Stock keeping unit</summary>
    public string Sku { get; set; }

    /// <summary>Indicates if product is active</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for creating a new product
/// </summary>
public class CreateProductDto
{
    /// <summary>Product name (required, max 100 characters)</summary>
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string Name { get; set; }

    /// <summary>Product description (optional)</summary>
    public string? Description { get; set; }

    /// <summary>Product price (required, must be positive)</summary>
    [System.ComponentModel.DataAnnotations.Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    /// <summary>Stock keeping unit (required, unique)</summary>
    [System.ComponentModel.DataAnnotations.Required]
    public string Sku { get; set; }
}

/// <summary>
/// DTO for updating an existing product
/// </summary>
public class UpdateProductDto
{
    /// <summary>Product name</summary>
    public string? Name { get; set; }

    /// <summary>Product description</summary>
    public string? Description { get; set; }

    /// <summary>Product price</summary>
    public decimal? Price { get; set; }

    /// <summary>Indicates if product is active</summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Generic paginated result
/// </summary>
public class PagedResultDto<T>
{
    /// <summary>Items in the current page</summary>
    public List<T> Items { get; set; }

    /// <summary>Total number of items across all pages</summary>
    public int Total { get; set; }

    /// <summary>Current page number</summary>
    public int Page { get; set; }

    /// <summary>Items per page</summary>
    public int PageSize { get; set; }
}
