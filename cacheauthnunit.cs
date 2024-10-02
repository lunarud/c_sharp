using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly string _authToken;

    public ApiClient(HttpClient httpClient, IMemoryCache cache, string authToken)
    {
        _httpClient = httpClient;
        _cache = cache;
        _authToken = authToken;
    }

    public async Task<string> GetDataAsync(string endpoint)
    {
        if (_cache.TryGetValue(endpoint, out string cachedData))
        {
            return cachedData;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("Authorization", $"Bearer {_authToken}");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadAsStringAsync();

        // Cache the result for future requests
        _cache.Set(endpoint, data);

        return data;
    }
}


using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using NUnit.Framework;

[TestFixture]
public class ApiClientTests
{
    [Test]
    public async Task GetDataAsync_UsesCache_IfDataIsCached()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cachedValue = "cached result";
        cache.Set("https://example.com/data", cachedValue);

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);

        var apiClient = new ApiClient(httpClient, cache, "test-token");

        // Act
        var result = await apiClient.GetDataAsync("https://example.com/data");

        // Assert
        Assert.AreEqual(cachedValue, result);
        mockHttpMessageHandler.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task GetDataAsync_CallsApi_AndAddsToCache()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("api response")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);

        var apiClient = new ApiClient(httpClient, cache, "test-token");

        // Act
        var result = await apiClient.GetDataAsync("https://example.com/data");

        // Assert
        Assert.AreEqual("api response", result);
        Assert.IsTrue(cache.TryGetValue("https://example.com/data", out string cachedData));
        Assert.AreEqual("api response", cachedData);

        mockHttpMessageHandler.Protected()
            .Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri == new System.Uri("https://example.com/data") &&
                req.Headers.Authorization.ToString() == "Bearer test-token"
            ), ItExpr.IsAny<CancellationToken>());
    }
}
