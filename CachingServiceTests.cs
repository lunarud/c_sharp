using System;
using System.Threading.Tasks;
 
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;

namespace CachingServiceTests.Tests
{
    [TestFixture]
    public class CachingServiceTests
    {
        private CachingService _cachingService;
        private Mock<ICacheProvider> _cacheProviderMock;

        [SetUp]
        public void Setup()
        {
            _cacheProviderMock = new Mock<ICacheProvider>();
            _cachingService = new CachingService(_cacheProviderMock.Object);
        }

        [Test]
        public void Add_ShouldAddItemToCache()
        {
            // Arrange
            var key = "test_key";
            var item = "test_item";
            var options = new MemoryCacheEntryOptions();

            // Act
            _cachingService.Add(key, item, options);

            // Assert
            _cacheProviderMock.Verify(p => p.Set(key, item, options), Times.Once);
        }

        [Test]
        public void Get_ShouldReturnCachedItem()
        {
            // Arrange
            var key = "test_key";
            var cachedItem = "test_item";
            _cacheProviderMock.Setup(p => p.Get(key)).Returns(cachedItem);

            // Act
            var result = _cachingService.Get<string>(key);

            // Assert
            Assert.AreEqual(cachedItem, result);
            _cacheProviderMock.Verify(p => p.Get(key), Times.Once);
        }

        [Test]
        public void Get_ShouldThrowException_WhenKeyIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => _cachingService.Get<string>(null));
        }

        [Test]
        public void GetOrAdd_ShouldAddNewItemToCache_WhenNotPresent()
        {
            // Arrange
            var key = "test_key";
            var item = "test_item";
            _cacheProviderMock.Setup(p => p.GetOrCreate<object>(It.IsAny<string>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<Func<ICacheEntry, object>>()))
                .Returns(new Lazy<string>(() => item));

            // Act
            var result = _cachingService.GetOrAdd(key, entry => item);

            // Assert
            Assert.AreEqual(item, result);
            _cacheProviderMock.Verify(p => p.GetOrCreate<object>(It.IsAny<string>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<Func<ICacheEntry, object>>()), Times.Once);
        }

        [Test]
        public void Remove_ShouldRemoveItemFromCache()
        {
            // Arrange
            var key = "test_key";

            // Act
            _cachingService.Remove(key);

            // Assert
            _cacheProviderMock.Verify(p => p.Remove(key), Times.Once);
        }

        [Test]
        public async Task GetAsync_ShouldReturnCachedItem()
        {
            // Arrange
            var key = "test_key";
            var cachedItem = Task.FromResult("test_item");
            _cacheProviderMock.Setup(p => p.Get(key)).Returns(cachedItem);

            // Act
            var result = await _cachingService.GetAsync<string>(key);

            // Assert
            Assert.AreEqual("test_item", result);
            _cacheProviderMock.Verify(p => p.Get(key), Times.Once);
        }

        [Test]
        public async Task GetOrAddAsync_ShouldAddNewItemToCache_WhenNotPresent()
        {
            // Arrange
            var key = "test_key";
            var item = "test_item";
            _cacheProviderMock.Setup(p => p.GetOrCreate<object>(It.IsAny<string>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<Func<ICacheEntry, object>>()))
                .Returns(new AsyncLazy<string>(async () => await Task.FromResult(item)));

            // Act
            var result = await _cachingService.GetOrAddAsync(key, async entry => await Task.FromResult(item));

            // Assert
            Assert.AreEqual(item, result);
            _cacheProviderMock.Verify(p => p.GetOrCreate<object>(It.IsAny<string>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<Func<ICacheEntry, object>>()), Times.Once);
        }

        [Test]
        public void TryGetValue_ShouldReturnTrue_WhenItemIsPresentInCache()
        {
            // Arrange
            var key = "test_key";
            var item = "test_item";
            _cacheProviderMock.Setup(p => p.TryGetValue(key, out item)).Returns(true);

            // Act
            var result = _cachingService.TryGetValue(key, out string cachedItem);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(item, cachedItem);
            _cacheProviderMock.Verify(p => p.TryGetValue(key, out item), Times.Once);
        }

        [Test]
        public void TryGetValue_ShouldReturnFalse_WhenItemIsNotInCache()
        {
            // Arrange
            var key = "test_key";
            string item = null;
            _cacheProviderMock.Setup(p => p.TryGetValue(key, out item)).Returns(false);

            // Act
            var result = _cachingService.TryGetValue(key, out string cachedItem);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(cachedItem);
            _cacheProviderMock.Verify(p => p.TryGetValue(key, out item), Times.Once);
        }
    }
}
