using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Configuration
public class ApiClientConfiguration
{
    public string BaseUrl { get; set; } = "https://localhost:8080/api";
    public int TimeoutSeconds { get; set; } = 30;
    public string ApiKey { get; set; } = string.Empty;
}

// Base Entity Model
public abstract class BaseEntity
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Example Product Entity
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> Tags { get; set; } = new();
}

// API Response Models
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class PagedResponse<T>
{
    public List<T> Content { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public long TotalElements { get; set; }
    public int TotalPages { get; set; }
    public bool First { get; set; }
    public bool Last { get; set; }
}

// Custom Exceptions
public class ApiException : Exception
{
    public int StatusCode { get; }
    public string? ResponseContent { get; }

    public ApiException(int statusCode, string message, string? responseContent = null) 
        : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }
}

// Generic CRUD Client Interface
public interface ICrudApiClient<T> where T : BaseEntity
{
    Task<ApiResponse<T>> CreateAsync(T entity);
    Task<ApiResponse<T>> GetByIdAsync(string id);
    Task<ApiResponse<PagedResponse<T>>> GetAllAsync(int page = 0, int size = 20, string? sortBy = null, string? sortDirection = "asc");
    Task<ApiResponse<T>> UpdateAsync(string id, T entity);
    Task<ApiResponse<bool>> DeleteAsync(string id);
    Task<ApiResponse<List<T>>> SearchAsync(string query, int page = 0, int size = 20);
}

// Generic CRUD API Client Implementation
public class CrudApiClient<T> : ICrudApiClient<T> where T : BaseEntity
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CrudApiClient<T>> _logger;
    private readonly string _entityEndpoint;
    private readonly JsonSerializerOptions _jsonOptions;

    public CrudApiClient(
        HttpClient httpClient, 
        ILogger<CrudApiClient<T>> logger,
        IOptions<ApiClientConfiguration> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _entityEndpoint = typeof(T).Name.ToLowerInvariant() + "s"; // e.g., "products"
        
        _httpClient.BaseAddress = new Uri(config.Value.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(config.Value.TimeoutSeconds);
        
        if (!string.IsNullOrEmpty(config.Value.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", config.Value.ApiKey);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ApiResponse<T>> CreateAsync(T entity)
    {
        try
        {
            _logger.LogInformation("Creating new {EntityType}", typeof(T).Name);
            
            var json = JsonSerializer.Serialize(entity, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(_entityEndpoint, content);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public async Task<ApiResponse<T>> GetByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Getting {EntityType} with ID: {Id}", typeof(T).Name, id);
            
            var response = await _httpClient.GetAsync($"{_entityEndpoint}/{id}");
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {EntityType} with ID: {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public async Task<ApiResponse<PagedResponse<T>>> GetAllAsync(
        int page = 0, 
        int size = 20, 
        string? sortBy = null, 
        string? sortDirection = "asc")
    {
        try
        {
            _logger.LogInformation("Getting all {EntityType} - Page: {Page}, Size: {Size}", typeof(T).Name, page, size);
            
            var queryParams = new List<string>
            {
                $"page={page}",
                $"size={size}"
            };
            
            if (!string.IsNullOrEmpty(sortBy))
            {
                queryParams.Add($"sort={sortBy},{sortDirection}");
            }
            
            var queryString = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"{_entityEndpoint}?{queryString}");
            
            return await HandleResponse<PagedResponse<T>>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public async Task<ApiResponse<T>> UpdateAsync(string id, T entity)
    {
        try
        {
            _logger.LogInformation("Updating {EntityType} with ID: {Id}", typeof(T).Name, id);
            
            var json = JsonSerializer.Serialize(entity, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"{_entityEndpoint}/{id}", content);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {EntityType} with ID: {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(string id)
    {
        try
        {
            _logger.LogInformation("Deleting {EntityType} with ID: {Id}", typeof(T).Name, id);
            
            var response = await _httpClient.DeleteAsync($"{_entityEndpoint}/{id}");
            return await HandleResponse<bool>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {EntityType} with ID: {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public async Task<ApiResponse<List<T>>> SearchAsync(string query, int page = 0, int size = 20)
    {
        try
        {
            _logger.LogInformation("Searching {EntityType} with query: {Query}", typeof(T).Name, query);
            
            var encodedQuery = Uri.EscapeDataString(query);
            var response = await _httpClient.GetAsync($"{_entityEndpoint}/search?q={encodedQuery}&page={page}&size={size}");
            
            return await HandleResponse<List<T>>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching {EntityType} with query: {Query}", typeof(T).Name, query);
            throw;
        }
    }

    private async Task<ApiResponse<TResponse>> HandleResponse<TResponse>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        
        if (response.IsSuccessStatusCode)
        {
            try
            {
                // Handle direct entity responses (for single items)
                if (typeof(TResponse) == typeof(T) || typeof(TResponse).IsSubclassOf(typeof(BaseEntity)))
                {
                    var entity = JsonSerializer.Deserialize<TResponse>(content, _jsonOptions);
                    return new ApiResponse<TResponse>
                    {
                        Success = true,
                        Data = entity,
                        Message = "Operation completed successfully"
                    };
                }
                
                // Handle wrapped responses
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<TResponse>>(content, _jsonOptions);
                return apiResponse ?? new ApiResponse<TResponse>
                {
                    Success = false,
                    Message = "Invalid response format"
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing response: {Content}", content);
                return new ApiResponse<TResponse>
                {
                    Success = false,
                    Message = "Invalid JSON response",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
        
        var errorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
        throw new ApiException((int)response.StatusCode, errorMessage, content);
    }
}

// Product-specific client with additional methods
public interface IProductApiClient : ICrudApiClient<Product>
{
    Task<ApiResponse<List<Product>>> GetByCategory(string category);
    Task<ApiResponse<List<Product>>> GetActiveProducts();
    Task<ApiResponse<Product>> UpdatePrice(string id, decimal newPrice);
}

public class ProductApiClient : CrudApiClient<Product>, IProductApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductApiClient(
        HttpClient httpClient, 
        ILogger<ProductApiClient> logger,
        IOptions<ApiClientConfiguration> config) 
        : base(httpClient, logger, config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ApiResponse<List<Product>>> GetByCategory(string category)
    {
        try
        {
            _logger.LogInformation("Getting products by category: {Category}", category);
            
            var encodedCategory = Uri.EscapeDataString(category);
            var response = await _httpClient.GetAsync($"products/category/{encodedCategory}");
            
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var products = JsonSerializer.Deserialize<List<Product>>(content, _jsonOptions);
                return new ApiResponse<List<Product>>
                {
                    Success = true,
                    Data = products ?? new List<Product>(),
                    Message = "Products retrieved successfully"
                };
            }
            
            throw new ApiException((int)response.StatusCode, $"HTTP {response.StatusCode}: {response.ReasonPhrase}", content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by category: {Category}", category);
            throw;
        }
    }

    public async Task<ApiResponse<List<Product>>> GetActiveProducts()
    {
        try
        {
            _logger.LogInformation("Getting active products");
            
            var response = await _httpClient.GetAsync("products/active");
            
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var products = JsonSerializer.Deserialize<List<Product>>(content, _jsonOptions);
                return new ApiResponse<List<Product>>
                {
                    Success = true,
                    Data = products ?? new List<Product>(),
                    Message = "Active products retrieved successfully"
                };
            }
            
            throw new ApiException((int)response.StatusCode, $"HTTP {response.StatusCode}: {response.ReasonPhrase}", content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active products");
            throw;
        }
    }

    public async Task<ApiResponse<Product>> UpdatePrice(string id, decimal newPrice)
    {
        try
        {
            _logger.LogInformation("Updating price for product {Id} to {Price}", id, newPrice);
            
            var priceUpdate = new { price = newPrice };
            var json = JsonSerializer.Serialize(priceUpdate, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PatchAsync($"products/{id}/price", content);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var product = JsonSerializer.Deserialize<Product>(responseContent, _jsonOptions);
                return new ApiResponse<Product>
                {
                    Success = true,
                    Data = product,
                    Message = "Price updated successfully"
                };
            }
            
            throw new ApiException((int)response.StatusCode, $"HTTP {response.StatusCode}: {response.ReasonPhrase}", responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating price for product {Id}", id);
            throw;
        }
    }
}

// Usage Example and Service Registration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiClients(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure the API client settings
        services.Configure<ApiClientConfiguration>(configuration.GetSection("ApiClient"));
        
        // Register HttpClient with proper configuration
        services.AddHttpClient<CrudApiClient<Product>>(client =>
        {
            var config = configuration.GetSection("ApiClient").Get<ApiClientConfiguration>();
            client.BaseAddress = new Uri(config?.BaseUrl ?? "https://localhost:8080/api");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        
        services.AddHttpClient<ProductApiClient>(client =>
        {
            var config = configuration.GetSection("ApiClient").Get<ApiClientConfiguration>();
            client.BaseAddress = new Uri(config?.BaseUrl ?? "https://localhost:8080/api");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        
        // Register the clients
        services.AddScoped<ICrudApiClient<Product>, CrudApiClient<Product>>();
        services.AddScoped<IProductApiClient, ProductApiClient>();
        
        return services;
    }
}

// Example Usage in a Controller or Service
public class ProductService
{
    private readonly IProductApiClient _productClient;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductApiClient productClient, ILogger<ProductService> logger)
    {
        _productClient = productClient;
        _logger = logger;
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        try
        {
            var response = await _productClient.CreateAsync(product);
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }
            throw new InvalidOperationException($"Failed to create product: {response.Message}");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error creating product");
            throw new InvalidOperationException($"API error: {ex.Message}");
        }
    }

    public async Task<Product?> GetProductAsync(string id)
    {
        try
        {
            var response = await _productClient.GetByIdAsync(id);
            return response.Success ? response.Data : null;
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
    }

    public async Task<PagedResponse<Product>> GetProductsAsync(int page = 0, int size = 20, string? sortBy = null)
    {
        var response = await _productClient.GetAllAsync(page, size, sortBy);
        return response.Data ?? new PagedResponse<Product>();
    }

    public async Task<List<Product>> GetProductsByCategoryAsync(string category)
    {
        var response = await _productClient.GetByCategory(category);
        return response.Data ?? new List<Product>();
    }

    public async Task<bool> UpdateProductAsync(string id, Product product)
    {
        try
        {
            var response = await _productClient.UpdateAsync(id, product);
            return response.Success;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error updating product {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteProductAsync(string id)
    {
        try
        {
            var response = await _productClient.DeleteAsync(id);
            return response.Success && response.Data == true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error deleting product {Id}", id);
            return false;
        }
    }
}
