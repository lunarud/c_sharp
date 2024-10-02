using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;

public class ApiClientTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly IMemoryCache _memoryCache;
    private readonly string _token = "dummyToken";
    private readonly string _apiUrl = "https://api.example.com/data";

    public ApiClientTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public async Task GetApiData_UsesAuthorizationTokenAndCache()
    {
        // Arrange
        var cacheKey = "ApiResponseCacheKey";
        var expectedResponse = "API Response Data";

        // Set up the cached data to simulate that no data is in cache initially
        _memoryCache.Remove(cacheKey);

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Headers.Authorization.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == _token),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedResponse),
            })
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var apiClient = new ApiClient(_httpClientFactoryMock.Object, _memoryCache, _token);

        // Act
        var result = await apiClient.GetApiData(_apiUrl);

        // Assert
        Assert.Equal(expectedResponse, result);

        // Ensure that the handler was only called once and caching works
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>()
        );

        // Make a second request and ensure it comes from cache
        var cachedResult = await apiClient.GetApiData(_apiUrl);
        Assert.Equal(expectedResponse, cachedResult);

        // Verify that SendAsync was not called again (because data was retrieved from cache)
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(), // still once after two calls, since second should use cache
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}

public class ApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly string _token;

    public ApiClient(IHttpClientFactory httpClientFactory, IMemoryCache cache, string token)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _token = token;
    }

    public async Task<string> GetApiData(string url)
    {
        var cacheKey = "ApiResponseCacheKey";

        if (!_cache.TryGetValue(cacheKey, out string cachedData))
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsStringAsync();

            _cache.Set(cacheKey, data, TimeSpan.FromMinutes(5)); // Cache the data for 5 minutes
            return data;
        }

        return cachedData;
    }
}
