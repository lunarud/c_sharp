using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using System;

[TestFixture]
public class CacheServiceTests
{
    private Mock<IMemoryCache> _mockMemoryCache;
    private CacheService _cacheService;

    [SetUp]
    public void Setup()
    {
        // Create a mock of the IMemoryCache
        _mockMemoryCache = new Mock<IMemoryCache>();

        // Initialize CacheService with the mocked IMemoryCache
        _cacheService = new CacheService(_mockMemoryCache.Object);
    }

    [Test]
    public void SetItemWithEvictionCallback_ShouldRegisterPostEvictionCallback()
    {
        // Arrange: Create a mock cache entry
        var mockCacheEntry = new Mock<ICacheEntry>();
        _mockMemoryCache.Setup(mc => mc.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

        // Act: Call the method that sets an item with the eviction callback
        _cacheService.SetItemWithEvictionCallback("testKey", "testValue");

        // Assert: Verify that the eviction callback was registered on the cache entry
        mockCacheEntry.VerifySet(
            entry => entry.PostEvictionCallbacks = It.IsAny<IList<PostEvictionCallbackRegistration>>(), 
            Times.Once);
    }

    [Test]
    public void PostEvictionCallback_ShouldBeCalledOnCacheEviction()
    {
        // Arrange: Create a mock cache entry and a callback to capture the eviction
        var mockCacheEntry = new Mock<ICacheEntry>();
        var evictionCallbackWasCalled = false;

        // Set up the mock memory cache to return the mock cache entry
        _mockMemoryCache.Setup(mc => mc.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

        // Simulate the eviction callback registration
        mockCacheEntry.SetupSet(entry => entry.PostEvictionCallbacks = It.IsAny<IList<PostEvictionCallbackRegistration>>())
            .Callback<IList<PostEvictionCallbackRegistration>>(callbacks =>
            {
                // Simulate the actual eviction callback being invoked
                foreach (var callbackRegistration in callbacks)
                {
                    callbackRegistration.EvictionCallback.Invoke("testKey", "testValue", EvictionReason.Expired, _cacheService);
                    evictionCallbackWasCalled = true;
                }
            });

        // Act: Call the method that sets the cache with an eviction callback
        _cacheService.SetItemWithEvictionCallback("testKey", "testValue");

        // Assert: Verify that the eviction callback was called
        Assert.IsTrue(evictionCallbackWasCalled);
    }
}
