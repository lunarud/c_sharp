using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

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
    public void SetItemWithEvictionCallback_ShouldRegisterPostEvictionCallback_Alternative()
    {
        // Arrange: Create a mock cache entry
        var mockCacheEntry = new Mock<ICacheEntry>();

        // This will hold the registered PostEvictionCallbacks to inspect later
        List<PostEvictionCallbackRegistration> registeredCallbacks = null;

        // Set up the mock to return the mock cache entry
        _mockMemoryCache.Setup(mc => mc.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

        // Use Callback to capture the eviction callbacks that are being set
        mockCacheEntry.SetupSet(entry => entry.PostEvictionCallbacks = It.IsAny<IList<PostEvictionCallbackRegistration>>())
            .Callback<IList<PostEvictionCallbackRegistration>>(callbacks =>
            {
                registeredCallbacks = (List<PostEvictionCallbackRegistration>)callbacks;
            });

        // Act: Call the method that sets an item with an eviction callback
        _cacheService.SetItemWithEvictionCallback("testKey", "testValue");

        // Assert: Ensure that the eviction callback was registered and capture it correctly
        Assert.IsNotNull(registeredCallbacks);
        Assert.IsNotEmpty(registeredCallbacks);
        Assert.AreEqual("testKey", registeredCallbacks[0].EvictionCallback.Target);
    }

    [Test]
    public void PostEvictionCallback_ShouldBeCalledOnCacheEviction_Alternative()
    {
        // Arrange: Create a mock cache entry and a callback to capture the eviction
        var mockCacheEntry = new Mock<ICacheEntry>();
        var evictionCallbackWasCalled = false;

        // Set up the mock memory cache to return the mock cache entry
        _mockMemoryCache.Setup(mc => mc.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

        // Capture the eviction callback registration
        List<PostEvictionCallbackRegistration> registeredCallbacks = null;
        mockCacheEntry.SetupSet(entry => entry.PostEvictionCallbacks = It.IsAny<IList<PostEvictionCallbackRegistration>>())
            .Callback<IList<PostEvictionCallbackRegistration>>(callbacks =>
            {
                registeredCallbacks = (List<PostEvictionCallbackRegistration>)callbacks;
            });

        // Act: Call the method that sets the cache with an eviction callback
        _cacheService.SetItemWithEvictionCallback("testKey", "testValue");

        // Simulate the eviction
        foreach (var callbackRegistration in registeredCallbacks)
        {
            callbackRegistration.EvictionCallback.Invoke("testKey", "testValue", EvictionReason.Expired, _cacheService);
            evictionCallbackWasCalled = true;
        }

        // Assert: Verify that the eviction callback was called
        Assert.IsTrue(evictionCallbackWasCalled);
    }
}
