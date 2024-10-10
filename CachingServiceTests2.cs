using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using System;

namespace CachingServiceTests.Tests
{
    [TestFixture]
    public class CachingServiceTests
    {
        private CachingService _cachingService;
        private Mock<ICacheProvider> _cacheProviderMock;
        private Mock<ICacheEntry> _cacheEntryMock;

        [SetUp]
        public void Setup()
        {
            _cacheProviderMock = new Mock<ICacheProvider>();
            _cachingService = new CachingService(_cacheProviderMock.Object);
            _cacheEntryMock = new Mock<ICacheEntry>();
        }

        [Test]
        public void GetOrAdd_ShouldUseCacheEntryCorrectly()
        {
            // Arrange
            var key = "test_key";
            var item = "test_item";
            _cacheProviderMock
                .Setup(p => p.GetOrCreate(It.IsAny<string>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<Func<ICacheEntry, object>>()))
                .Returns<string, MemoryCacheEntryOptions, Func<ICacheEntry, object>>((k, options, factory) => factory(_cacheEntryMock.Object));

            // Mocking relevant properties of ICacheEntry
            _cacheEntryMock.SetupProperty(e => e.AbsoluteExpirationRelativeToNow, TimeSpan.FromMinutes(10));

            // Act
            var result = _cachingService.GetOrAdd(key, entry => item);

            // Assert
            Assert.AreEqual(item, result);
            _cacheProviderMock.Verify(p => p.GetOrCreate(It.IsAny<string>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<Func<ICacheEntry, object>>()), Times.Once);
            Assert.AreEqual(TimeSpan.FromMinutes(10), _cacheEntryMock.Object.AbsoluteExpirationRelativeToNow);
        }

        [Test]
        public void GetOrAdd_ShouldRegisterPostEvictionCallback()
        {
            // Arrange
            var key = "test_key";
            var item = "test_item";

            // Setup the mock to call the cache factory when GetOrCreate is invoked
            _cacheProviderMock
                .Setup(p => p.GetOrCreate(It.IsAny<string>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<Func<ICacheEntry, object>>()))
                .Returns<string, MemoryCacheEntryOptions, Func<ICacheEntry, object>>((k, options, factory) => factory(_cacheEntryMock.Object));

            // Mock the PostEvictionCallbacks collection and behavior
            var postEvictionCallbackRegistrationMock = new Mock<PostEvictionCallbackRegistration>();
            var evictionCallbacks = new System.Collections.Generic.List<PostEvictionCallbackRegistration>();
            _cacheEntryMock.Setup(e => e.PostEvictionCallbacks).Returns(evictionCallbacks);

            // Act
            var result = _cachingService.GetOrAdd(key, entry => item);

            // Assert
            Assert.AreEqual(item, result);
            Assert.IsNotEmpty(evictionCallbacks); // Verifies that a callback was registered
        }
    }
}
