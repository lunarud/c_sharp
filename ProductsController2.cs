using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] Product product)
    {
        try
        {
            var createdProduct = await _productService.CreateProductAsync(product);
            return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to create product");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating product");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(string id)
    {
        try
        {
            var product = await _productService.GetProductAsync(id);
            if (product == null)
            {
                return NotFound($"Product with ID {id} not found");
            }
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<Product>>> GetProducts(
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        [FromQuery] string? sortBy = null)
    {
        try
        {
            var products = await _productService.GetProductsAsync(page, size, sortBy);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("category/{category}")]
    public async Task<ActionResult<List<Product>>> GetProductsByCategory(string category)
    {
        try
        {
            var products = await _productService.GetProductsByCategoryAsync(category);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by category {Category}", category);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> UpdateProduct(string id, [FromBody] Product product)
    {
        try
        {
            var success = await _productService.UpdateProductAsync(id, product);
            if (!success)
            {
                return BadRequest("Failed to update product");
            }

            var updatedProduct = await _productService.GetProductAsync(id);
            return Ok(updatedProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(string id)
    {
        try
        {
            var success = await _productService.DeleteProductAsync(id);
            if (!success)
            {
                return NotFound($"Product with ID {id} not found or could not be deleted");
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<Product>>> SearchProducts(
        [FromQuery] string query,
        [FromQuery] int page = 0,
        [FromQuery] int size = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty");
            }

            // Use the generic search from the base client
            var productClient = HttpContext.RequestServices.GetRequiredService<IProductApiClient>();
            var response = await productClient.SearchAsync(query, page, size);
            
            if (response.Success && response.Data != null)
            {
                return Ok(response.Data);
            }
            
            return BadRequest(response.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with query {Query}", query);
            return StatusCode(500, "Internal server error");
        }
    }
}

// Example of a specialized controller for advanced operations
[ApiController]
[Route("api/[controller]")]
public class ProductManagementController : ControllerBase
{
    private readonly IProductApiClient _productClient;
    private readonly ILogger<ProductManagementController> _logger;

    public ProductManagementController(IProductApiClient productClient, ILogger<ProductManagementController> logger)
    {
        _productClient = productClient;
        _logger = logger;
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<Product>>> GetActiveProducts()
    {
        try
        {
            var response = await _productClient.GetActiveProducts();
            if (response.Success && response.Data != null)
            {
                return Ok(response.Data);
            }
            return BadRequest(response.Message);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error getting active products");
            return StatusCode(ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting active products");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("{id}/price")]
    public async Task<ActionResult<Product>> UpdateProductPrice(string id, [FromBody] decimal newPrice)
    {
        try
        {
            if (newPrice <= 0)
            {
                return BadRequest("Price must be greater than zero");
            }

            var response = await _productClient.UpdatePrice(id, newPrice);
            if (response.Success && response.Data != null)
            {
                return Ok(response.Data);
            }
            return BadRequest(response.Message);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error updating price for product {Id}", id);
            return StatusCode(ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating price for product {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<Product>>> CreateBulkProducts([FromBody] List<Product> products)
    {
        try
        {
            var createdProducts = new List<Product>();
            var errors = new List<string>();

            foreach (var product in products)
            {
                try
                {
                    var response = await _productClient.CreateAsync(product);
                    if (response.Success && response.Data != null)
                    {
                        createdProducts.Add(response.Data);
                    }
                    else
                    {
                        errors.Add($"Failed to create product '{product.Name}': {response.Message}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error creating product '{product.Name}': {ex.Message}");
                }
            }

            if (errors.Any())
            {
                return BadRequest(new { 
                    CreatedProducts = createdProducts, 
                    Errors = errors,
                    Message = $"Created {createdProducts.Count} out of {products.Count} products"
                });
            }

            return Ok(createdProducts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk product creation");
            return StatusCode(500, "Internal server error");
        }
    }
}

// Health check controller for monitoring API connectivity
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IProductApiClient _productClient;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IProductApiClient productClient, ILogger<HealthController> logger)
    {
        _productClient = productClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> CheckHealth()
    {
        try
        {
            // Try to get a page of products to test connectivity
            var response = await _productClient.GetAllAsync(0, 1);
            
            if (response.Success)
            {
                return Ok(new { 
                    Status = "Healthy", 
                    Timestamp = DateTime.UtcNow,
                    ApiConnectivity = "OK"
                });
            }
            
            return StatusCode(503, new { 
                Status = "Unhealthy", 
                Timestamp = DateTime.UtcNow,
                ApiConnectivity = "Failed",
                Message = response.Message
            });
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "API health check failed");
            return StatusCode(503, new { 
                Status = "Unhealthy", 
                Timestamp = DateTime.UtcNow,
                ApiConnectivity = "Failed",
                StatusCode = ex.StatusCode,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check error");
            return StatusCode(503, new { 
                Status = "Unhealthy", 
                Timestamp = DateTime.UtcNow,
                ApiConnectivity = "Error",
                Message = "Internal server error"
            });
        }
    }
}
