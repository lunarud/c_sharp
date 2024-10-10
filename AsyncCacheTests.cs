using NUnit.Framework;
using System;
using System.Threading.Tasks;

[TestFixture]
public class AsyncCacheTests
{
    private AsyncCache _asyncCache;

    [SetUp]
    public void Setup()
    {
        _asyncCache = new AsyncCache();
    }

    [Test]
    public async Task AddAsync_Item_ShouldBeAdded()
    {
        // Arrange
        var cacheKey = new CacheKeys("Key1", DateTime.UtcNow.AddMinutes(5));

        // Act
        var result = await _asyncCache.AddAsync("Key1", cacheKey);

        // Assert
        Assert.IsTrue(result, "Item should be added successfully.");
    }

    [Test]
    public async Task GetAsync_ItemExists_ShouldReturnItem()
    {
        // Arrange
        var cacheKey = new CacheKeys("Key1", DateTime.UtcNow.AddMinutes(5));
        await _asyncCache.AddAsync("Key1", cacheKey);

        // Act
        var retrievedCacheKey = await _asyncCache.GetAsync("Key1");

        // Assert
        Assert.IsNotNull(retrievedCacheKey, "Retrieved item should not be null.");
        Assert.AreEqual("Key1", retrievedCacheKey.Key, "The key of the retrieved item should match.");
    }

    [Test]
    public async Task GetAsync_ItemDoesNotExist_ShouldReturnNull()
    {
        // Act
        var retrievedCacheKey = await _asyncCache.GetAsync("KeyNonExistent");

        // Assert
        Assert.IsNull(retrievedCacheKey, "Retrieved item should be null when it does not exist.");
    }

    [Test]
    public async Task RemoveAsync_ItemExists_ShouldBeRemoved()
    {
        // Arrange
        var cacheKey = new CacheKeys("Key1", DateTime.UtcNow.AddMinutes(5));
        await _asyncCache.AddAsync("Key1", cacheKey);

        // Act
        var result = await _asyncCache.RemoveAsync("Key1");

        // Assert
        Assert.IsTrue(result, "Item should be removed successfully.");
    }

    [Test]
    public async Task GetCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var cacheKey1 = new CacheKeys("Key1", DateTime.UtcNow.AddMinutes(5));
        var cacheKey2 = new CacheKeys("Key2", DateTime.UtcNow.AddMinutes(10));

        await _asyncCache.AddAsync("Key1", cacheKey1);
        await _asyncCache.AddAsync("Key2", cacheKey2);

        // Act
        var count = await _asyncCache.GetCountAsync();

        // Assert
        Assert.AreEqual(2, count, "The cache should contain 2 items.");
    }
}
