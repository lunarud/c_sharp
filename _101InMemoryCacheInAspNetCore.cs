using _101InMemoryCacheInAspNetCore.Controllers;
using _101InMemoryCacheInAspNetCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using System;

namespace _101InMemoryCacheInAspNetCore.Tests
{
    [TestFixture]
    public class ProductsControllerTests
    {
        private ProductsController _controller;
        private Mock<IMemoryCache> _memoryCacheMock;

        [SetUp]
        public void Setup()
        {
            // Create a mock for IMemoryCache
            _memoryCacheMock = new Mock<IMemoryCache>();

            // Instantiate ProductsController with the mock memory cache
            _controller = new ProductsController(_memoryCacheMock.Object);
        }

        [Test]
        public void Get_ReturnsCachedProduct_WhenProductIsInCache()
        {
            // Arrange
            var productId = 1;
            var cachedProduct = new Product { Id = productId, Name = $"Product {productId}", Price = 99.99M };

            // Mock cache retrieval (TryGetValue should return true and output the cached product)
            _memoryCacheMock.Setup(mc => mc.TryGetValue($"Product_{productId}", out cachedProduct)).Returns(true);

            // Act
            var result = _controller.Get(productId) as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual(cachedProduct, result.Value);

            // Ensure that the service never tries to create a new product if the product is in the cache
            _memoryCacheMock.Verify(mc => mc.Set(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<MemoryCacheEntryOptions>()), Times.Never);
        }

        [Test]
        public void Get_ReturnsNewProductAndCachesIt_WhenProductIsNotInCache()
        {
            // Arrange
            var productId = 2;
            Product cachedProduct = null;  // Simulate that the product is not in the cache

            // Set up TryGetValue to return false (product not in cache) and output null
            _memoryCacheMock.Setup(mc => mc.TryGetValue($"Product_{productId}", out cachedProduct)).Returns(false);

            // Set up the cache Set method to do nothing (just for verification later)
            _memoryCacheMock.Setup(mc => mc.Set($"Product_{productId}", It.IsAny<Product>(), It.IsAny<TimeSpan>()));

            // Act
            var result = _controller.Get(productId) as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);

            var returnedProduct = result.Value as Product;
            Assert.IsNotNull(returnedProduct);
            Assert.AreEqual(productId, returnedProduct.Id);
            Assert.AreEqual($"Product {productId}", returnedProduct.Name);
            Assert.AreEqual(99.99M, returnedProduct.Price);

            // Verify that the product was added to the cache
            _memoryCacheMock.Verify(mc => mc.Set($"Product_{productId}", It.Is<Product>(p => p.Id == productId), It.IsAny<TimeSpan>()), Times.Once);
        }
    }
}
