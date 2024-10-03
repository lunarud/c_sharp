using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace CachingTests
{
    public class Product { public int Id { get; set; } public string Name { get; set; } public decimal Price { get; set; } }

    public class ProductsController
    {
        private readonly ICacheProvider _cacheProvider;

        public ProductsController(ICacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }

        public async Task<Product> GetProductAsync(int id)
        {
            var product = await _cacheProvider.GetAsync<Product>($"Product_{id}");
            if (product != null)
            {
                return product;
            }
            else
            {
                var newProduct = new Product { Id = id, Name = $"Product {id}", Price = 99.99M };
                await _cacheProvider.SetAsync($"Product_{id}", newProduct, TimeSpan.FromMinutes(10));
                return newProduct;
            }
        }
    }

    [TestFixture]
    public class ProductsControllerTests
    {
        private Mock<ICacheProvider> _cacheProviderMock;
        private ProductsController _controller;

        [SetUp]
        public void Setup()
        {
            _cacheProviderMock = new Mock<ICacheProvider>();
            _controller = new ProductsController(_cacheProviderMock.Object);
        }

        [Test]
        public async Task GetProductAsync_ReturnsCachedProduct_WhenProductIsInCache()
        {
            // Arrange
            var productId = 1;
            var cachedProduct = new Product { Id = productId, Name = $"Product {productId}", Price = 99.99M };

            _cacheProviderMock.Setup(cp => cp.GetAsync<Product>($"Product_{productId}"))
                .ReturnsAsync(cachedProduct);

            // Act
            var result = await _controller.GetProductAsync(productId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(cachedProduct, result);

            // Ensure that SetAsync was never called (since the product was cached)
            _cacheProviderMock.Verify(cp => cp.SetAsync(It.IsAny<string>(), It.IsAny<Product>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Test]
        public async Task GetProductAsync_ReturnsNewProductAndCachesIt_WhenProductIsNotInCache()
        {
            // Arrange
            var productId = 2;
            Product cachedProduct = null;

            _cacheProviderMock.Setup(cp => cp.GetAsync<Product>($"Product_{productId}"))
                .ReturnsAsync(cachedProduct);

            // Simulate SetAsync call
            _cacheProviderMock.Setup(cp => cp.SetAsync($"Product_{productId}", It.IsAny<Product>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.GetProductAsync(productId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(productId, result.Id);
            Assert.AreEqual($"Product {productId}", result.Name);
            Assert.AreEqual(99.99M, result.Price);

            // Verify that SetAsync was called once
            _cacheProviderMock.Verify(cp => cp.SetAsync($"Product_{productId}", It.Is<Product>(p => p.Id == productId), It.IsAny<TimeSpan>()), Times.Once);
        }
    }
}
