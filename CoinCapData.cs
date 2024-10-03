using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

[TestFixture]
public class WeatherServiceTests
{
    private Mock<IMemoryCache> _cacheMock;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private HttpClient _httpClient;
    private WeatherService _weatherService;

    [SetUp]
    public void Setup()
    {
        _cacheMock = new Mock<IMemoryCache>();

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _weatherService = new WeatherService(_cacheMock.Object, _httpClient);
    }

    [Test]
    public async Task GetWeatherDataAsync_ShouldReturnCachedData_WhenCacheHit()
    {
        // Arrange
        var cachedData = "Sunny";
        _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedData)).Returns(true);

        // Act
        var result = await _weatherService.GetWeatherDataAsync();

        // Assert
        Assert.AreEqual(cachedData, result);
        _cacheMock.Verify(x => x.TryGetValue(It.IsAny<object>(), out cachedData), Times.Once);
        _httpMessageHandlerMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GetWeatherDataAsync_ShouldCallApiAndCacheResponse_WhenCacheMiss()
    {
        // Arrange
        var cachedData = (string)null; // No cached data
        _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedData)).Returns(false);

        var apiResponseContent = new StringContent("Rainy");
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = apiResponseContent };

        _httpMessageHandlerMock
            .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMessage);

        _cacheMock.Setup(x => x.Set(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
            .Returns((object key, object value, TimeSpan ts) => value);

        // Act
        var result = await _weatherService.GetWeatherDataAsync();

        // Assert
        Assert.AreEqual("Rainy", result);
        _httpMessageHandlerMock.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(x => x.Set(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Once);
    }
}
