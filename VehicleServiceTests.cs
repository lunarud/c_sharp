using CachingInmemory.Models;
using CachingInmemory.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CachingInmemory.Tests
{
    [TestFixture]
    public class VehicleServiceTests
    {
        private Mock<IMemoryCache> _memoryCacheMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClient;
        private VehicleService _vehicleService;

        [SetUp]
        public void SetUp()
        {
            _memoryCacheMock = new Mock<IMemoryCache>();

            // Mock the HttpMessageHandler to simulate HttpClient behavior
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            // Create a new HttpClient with the mocked handler
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

            // Create an instance of VehicleService with the mocked IMemoryCache and HttpClient
            _vehicleService = new VehicleService(_memoryCacheMock.Object, _httpClient);
        }

        [Test]
        public async Task GetVehicles_ReturnsVehiclesFromApi_WhenNotCached()
        {
            // Arrange
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 1, Name = "Car" },
                new Vehicle { Id = 2, Name = "Truck" }
            };

            var apiResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(vehicles))
            };

            // Setup the handler to return the mock response for any request
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(apiResponse);

            // Setup cache to return null (simulate cache miss)
            object cacheEntry = null;
            _memoryCacheMock.Setup(mc => mc.TryGetValue(It.IsAny<object>(), out cacheEntry)).Returns(false);

            // Act
            var result = _vehicleService.GetVehicles();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Car", result[0].Name);
            Assert.AreEqual("Truck", result[1].Name);

            // Verify that the Set method was called to cache the result
            _memoryCacheMock.Verify(mc => mc.Set(
                It.IsAny<object>(), 
                It.IsAny<object>(), 
                It.IsAny<MemoryCacheEntryOptions>()), 
                Times.Once);
        }

        [Test]
        public async Task GetVehicles_ReturnsCachedVehicles_WhenInCache()
        {
            // Arrange
            List<Vehicle> cachedVehicles = new List<Vehicle>
            {
                new Vehicle { Id = 3, Name = "Motorcycle" }
            };

            // Setup cache to return cached vehicles
            _memoryCacheMock.Setup(mc => mc.TryGetValue(It.IsAny<object>(), out cachedVehicles)).Returns(true);

            // Act
            var result = _vehicleService.GetVehicles();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Motorcycle", result[0].Name);

            // Verify that the Set method was never called since the data was cached
            _memoryCacheMock.Verify(mc => mc.Set(
                It.IsAny<object>(), 
                It.IsAny<object>(), 
                It.IsAny<MemoryCacheEntryOptions>()), 
                Times.Never);
        }
    }
}
