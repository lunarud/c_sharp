using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Threading.Tasks;
using Moq.Protected;
using System.Threading;
using System.Net;
using System.Net.Http.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CoinCapTests
{
    public class CoinCapData
    {
        public string Id { get; set; }
        public string Symbol { get; set; }
        public string CurrencySymbol { get; set; }
        public decimal RateUsd { get; set; }
    }

    public class CoinCapResponse
    {
        public CoinCapData Data { get; set; }
    }

    [TestFixture]
    public class CoinCapServiceTests
    {
        private Mock<IMemoryCache> _memoryCacheMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private CoinCapService _service;

        [SetUp]
        public void Setup()
        {
            _memoryCacheMock = new Mock<IMemoryCache>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _service = new CoinCapService(_memoryCacheMock.Object, _httpClientFactoryMock.Object);
        }

        [Test]
        public async Task GetCurrencyData_ReturnsCachedData_WhenDataExistsInCache()
        {
            // Arrange
            var coinCapData = new CoinCapData { Id = "bitcoin", Symbol = "BTC", RateUsd = 50000.0M };

            object cachedData = coinCapData;
            _memoryCacheMock.Setup(mc => mc.GetOrCreateAsync(It.IsAny<object>(), It.IsAny<Func<ICacheEntry, Task<CoinCapData>>>()))
                            .ReturnsAsync(coinCapData);

            // Act
            var result = await _service.GetCurrencyData("bitcoin");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(coinCapData, result);
            _memoryCacheMock.Verify(mc => mc.GetOrCreateAsync(It.IsAny<object>(), It.IsAny<Func<ICacheEntry, Task<CoinCapData>>>()), Times.Once);
        }

        [Test]
        public async Task GetCurrencyData_FetchesDataFromApiAndCaches_WhenCacheMiss()
        {
            // Arrange
            var coinCapData = new CoinCapData { Id = "bitcoin", Symbol = "BTC", RateUsd = 50000.0M };
            var coinCapResponse = new CoinCapResponse { Data = coinCapData };

            var apiResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(coinCapResponse))
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

            // Mock cache miss by returning null for GetOrCreateAsync
            CoinCapData cachedData = null;
            _memoryCacheMock.Setup(mc => mc.GetOrCreateAsync(It.IsAny<object>(), It.IsAny<Func<ICacheEntry, Task<CoinCapData>>>()))
                            .Returns<ICacheEntry, Func<ICacheEntry, Task<CoinCapData>>>((key, func) => func(It.IsAny<ICacheEntry>()));

            // Act
            var result = await _service.GetCurrencyData("bitcoin");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("bitcoin", result.Id);
            Assert.AreEqual("BTC", result.Symbol);
            Assert.AreEqual(50000.0M, result.RateUsd);

            // Verify cache miss and HTTP request
            _memoryCacheMock.Verify(mc => mc.GetOrCreateAsync(It.IsAny<object>(), It.IsAny<Func<ICacheEntry, Task<CoinCapData>>>()), Times.Once);
            httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }
    }

    public class CoinCapService
    {
        private readonly IMemoryCache memoryCache;
        private readonly IHttpClientFactory httpClientFactory;

        public CoinCapService(IMemoryCache memoryCache, IHttpClientFactory httpClientFactory)
        {
            this.memoryCache = memoryCache;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<CoinCapData> GetCurrencyData(string id)
        {
            return (await memoryCache.GetOrCreateAsync(
                $"{this.GetType().Name}.GetCurrencyData({id})",
                _ => GetData()))!;

            async Task<CoinCapData> GetData()
            {
                using var httpClient = httpClientFactory.CreateClient();
                var response = await httpClient.GetFromJsonAsync<CoinCapResponse>($"https://api.coincap.io/v2/rates/{id}");
                return response!.Data;
            }
        }
    }
}
