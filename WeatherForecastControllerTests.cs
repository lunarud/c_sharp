using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemoryCacheDemo.Tests
{
    public class WeatherForecastControllerTests
    {
        private WeatherForecastController _controller;
        private Mock<ILogger<WeatherForecastController>> _loggerMock;
        private Mock<IMemoryCache> _cacheMock;

        [SetUp]
        public void Setup()
        {
            // Create mock objects for ILogger and IMemoryCache
            _loggerMock = new Mock<ILogger<WeatherForecastController>>();
            _cacheMock = new Mock<IMemoryCache>();

            // Instantiate the controller with mocks
            _controller = new WeatherForecastController(_loggerMock.Object, _cacheMock.Object);
        }

        [Test]
        public void Get_WhenCacheIsEmpty_ReturnsNewWeatherForecasts()
        {
            // Arrange
            _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<List<WeatherForecast>>.IsAny)).Returns(false);

            // Act
            var result = _controller.Get();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(300, result.Count);
        }

        [Test]
        public void Get_WhenCacheIsNotEmpty_ReturnsCachedWeatherForecasts()
        {
            // Arrange
            var cachedForecasts = new List<WeatherForecast>
            {
                new WeatherForecast { Date = DateOnly.FromDateTime(DateTime.Now), TemperatureC = 20, Summary = "Cool" }
            };

            _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedForecasts)).Returns(true);

            // Act
            var result = _controller.Get();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(cachedForecasts, result);
        }

        [Test]
        public void Get_StoresWeatherForecastsInCache_WhenCacheIsEmpty()
        {
            // Arrange
            List<WeatherForecast> weathersFromCache = null;
            _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out weathersFromCache)).Returns(false);

            // Act
            var result = _controller.Get();

            // Assert
            _cacheMock.Verify(c => c.Set(It.IsAny<object>(), It.IsAny<List<WeatherForecast>>()), Times.Once);
        }
    }
}
