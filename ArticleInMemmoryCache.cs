using ArticleInMemmoryCache.Controllers;
using ArticleInMemmoryCache.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ArticleInMemmoryCache.Tests
{
    [TestFixture]
    public class ImprovedCountriesControllerWithCommentsTests
    {
        private Mock<IMemoryCache> _memoryCacheMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private ImprovedCountriesControllerWithComments _controller;

        [SetUp]
        public void Setup()
        {
            // Initialize the memory cache and http client factory mocks
            _memoryCacheMock = new Mock<IMemoryCache>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // Create the controller instance with the mocked dependencies
            _controller = new ImprovedCountriesControllerWithComments(_memoryCacheMock.Object, _httpClientFactoryMock.Object);
        }

        [Test]
        public async Task GetCountries_ReturnsCountriesFromCache_WhenCacheHit()
        {
            // Arrange
            var cachedCountries = new List<Country>
            {
                new Country { Name = "Country 1" },
                new Country { Name = "Country 2" }
            };

            // Setup the cache to return countries when cache key is accessed
            _memoryCacheMock.Setup(mc => mc.TryGetValue(It.IsAny<object>(), out cachedCountries)).Returns(true);

            // Act
            var result = await _controller.GetCountries() as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual(cachedCountries, result.Value);

            // Verify that the cache was accessed
            _memoryCacheMock.Verify(mc => mc.TryGetValue(It.IsAny<object>(), out cachedCountries), Times.Once);
        }

        [Test]
        public async Task GetCountries_FetchesAndCachesCountries_WhenCacheMiss()
        {
            // Arrange
            List<Country> cachedCountries = null;

            // Setup the cache to return false, simulating a cache miss
            _memoryCacheMock.Setup(mc => mc.TryGetValue(It.IsAny<object>(), out cachedCountries)).Returns(false);

            // Mock response data from the API
            var countriesFromApi = new List<Country>
            {
                new Country { Name = "Country 1" },
                new Country { Name = "Country 2" }
            };
            var apiResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(countriesFromApi))
            };

            // Mock HttpClient and HttpClientFactory
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(apiResponse);

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var result = await _controller.GetCountries() as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual(countriesFromApi, result.Value);

            // Verify the cache miss, and that cache is set with correct options
            _memoryCacheMock.Verify(mc => mc.TryGetValue(It.IsAny<object>(), out cachedCountries), Times.Once);
            _memoryCacheMock.Verify(mc => mc.Set(
                It.IsAny<object>(), 
                It.IsAny<List<Country>>(), 
                It.IsAny<MemoryCacheEntryOptions>()), 
                Times.Once);

            // Verify that the API was called
            httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
