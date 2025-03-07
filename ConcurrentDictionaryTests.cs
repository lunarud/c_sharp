using NUnit.Framework;
using System;
using System.Collections.Concurrent;

namespace ConcurrentDictionaryTests
{
    public class CacheKeys
    {
        public string Key { get; set; }
        public DateTime Expiration { get; set; }

        public CacheKeys(string key, DateTime expiration)
        {
            Key = key;
            Expiration = expiration;
        }
    }

    [TestFixture]
    public class ConcurrentDictionaryUnitTest
    {
        private ConcurrentDictionary<string, CacheKeys> _cacheDictionary;

        [SetUp]
        public void Setup()
        {
            // Initialize the dictionary before each test
            _cacheDictionary = new ConcurrentDictionary<string, CacheKeys>();
        }

        [Test]
        public void AddOrUpdate_Item_ShouldBeAdded()
        {
            // Arrange
            var cacheKey = new CacheKeys("Key1", DateTime.UtcNow.AddMinutes(5));

            // Act
            var result = _cacheDictionary.AddOrUpdate("Key1", cacheKey, (key, oldValue) => cacheKey);

            // Assert
            Assert.AreEqual(cacheKey, result);
            Assert.IsTrue(_cacheDictionary.ContainsKey("Key1"));
            Assert.AreEqual("Key1", _cacheDictionary["Key1"].Key);
        }

        [Test]
        public void TryGetValue_ItemExists_ShouldReturnTrue()
        {
            // Arrange
            var cacheKey = new CacheKeys("Key1", DateTime.UtcNow.AddMinutes(5));
            _cacheDictionary.TryAdd("Key1", cacheKey);

            // Act
            var success = _cacheDictionary.TryGetValue("Key1", out var retrievedCacheKey);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(cacheKey, retrievedCacheKey);
        }

        [Test]
        public void TryRemove_ItemExists_ShouldBeRemoved()
        {
            // Arrange
            var cacheKey = new CacheKeys("Key1", DateTime.UtcNow.AddMinutes(5));
            _cacheDictionary.TryAdd("Key1", cacheKey);

            // Act
            var success = _cacheDictionary.TryRemove("Key1", out var removedCacheKey);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(cacheKey, removedCacheKey);
            Assert.IsFalse(_cacheDictionary.ContainsKey("Key1"));
        }

        [Test]
        public void Count_ShouldReturnCorrectCount()
        {
            // Arrange
            var cacheKey1 = new CacheKeys("Key1", DateTime.UtcNow.AddMinutes(5));
            var cacheKey2 = new CacheKeys("Key2", DateTime.UtcNow.AddMinutes(10));
            _cacheDictionary.TryAdd("Key1", cacheKey1);
            _cacheDictionary.TryAdd("Key2", cacheKey2);

            // Act
            var count = _cacheDictionary.Count;

            // Assert
            Assert.AreEqual(2, count);
        }
    }
}

using NUnit.Framework;
using System.Collections.Concurrent;
using System.Collections.Generic;

[TestFixture]
public class ConcurrentDictionaryExtensionsTests
{
    [Test]
    public void CompareTo_EmptyDictionaries_ReturnsEmptyResult()
    {
        // Arrange
        var firstDict = new ConcurrentDictionary<string, int>();
        var secondDict = new ConcurrentDictionary<string, int>();

        // Act
        var result = firstDict.CompareTo(secondDict);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result.Added);
        Assert.IsEmpty(result.Updated);
        Assert.IsEmpty(result.Deleted);
    }

    [Test]
    public void CompareTo_AddedItems_DetectsNewItems()
    {
        // Arrange
        var firstDict = new ConcurrentDictionary<string, int>();
        var secondDict = new ConcurrentDictionary<string, int>
        {
            ["A"] = 1,
            ["B"] = 2
        };

        // Act
        var result = firstDict.CompareTo(secondDict);

        // Assert
        Assert.AreEqual(2, result.Added.Count);
        Assert.IsEmpty(result.Updated);
        Assert.IsEmpty(result.Deleted);
        Assert.AreEqual(1, result.Added["A"]);
        Assert.AreEqual(2, result.Added["B"]);
    }

    [Test]
    public void CompareTo_DeletedItems_DetectsRemovedItems()
    {
        // Arrange
        var firstDict = new ConcurrentDictionary<string, int>
        {
            ["A"] = 1,
            ["B"] = 2
        };
        var secondDict = new ConcurrentDictionary<string, int>();

        // Act
        var result = firstDict.CompareTo(secondDict);

        // Assert
        Assert.IsEmpty(result.Added);
        Assert.IsEmpty(result.Updated);
        Assert.AreEqual(2, result.Deleted.Count);
        Assert.AreEqual(1, result.Deleted["A"]);
        Assert.AreEqual(2, result.Deleted["B"]);
    }

    [Test]
    public void CompareTo_UpdatedItems_DetectsChanges()
    {
        // Arrange
        var firstDict = new ConcurrentDictionary<string, int>
        {
            ["A"] = 1,
            ["B"] = 2
        };
        var secondDict = new ConcurrentDictionary<string, int>
        {
            ["A"] = 1,
            ["B"] = 3
        };

        // Act
        var result = firstDict.CompareTo(secondDict);

        // Assert
        Assert.IsEmpty(result.Added);
        Assert.AreEqual(1, result.Updated.Count);
        Assert.IsEmpty(result.Deleted);
        Assert.AreEqual(2, result.Updated["B"].OldValue);
        Assert.AreEqual(3, result.Updated["B"].NewValue);
    }

    [Test]
    public void CompareTo_MixedChanges_DetectsAllDifferences()
    {
        // Arrange
        var firstDict = new ConcurrentDictionary<string, int>
        {
            ["A"] = 1,  // Will be deleted
            ["B"] = 2,  // Will be updated
            ["C"] = 3   // Will remain same
        };
        var secondDict = new ConcurrentDictionary<string, int>
        {
            ["B"] = 5,  // Updated
            ["C"] = 3,  // Same
            ["D"] = 4   // Added
        };

        // Act
        var result = firstDict.CompareTo(secondDict);

        // Assert
        // Added
        Assert.AreEqual(1, result.Added.Count);
        Assert.AreEqual(4, result.Added["D"]);

        // Updated
        Assert.AreEqual(1, result.Updated.Count);
        Assert.AreEqual(2, result.Updated["B"].OldValue);
        Assert.AreEqual(5, result.Updated["B"].NewValue);

        // Deleted
        Assert.AreEqual(1, result.Deleted.Count);
        Assert.AreEqual(1, result.Deleted["A"]);
    }

    [Test]
    public void CompareTo_SameDictionaries_ReturnsEmptyResult()
    {
        // Arrange
        var firstDict = new ConcurrentDictionary<string, int>
        {
            ["A"] = 1,
            ["B"] = 2
        };
        var secondDict = new ConcurrentDictionary<string, int>
        {
            ["A"] = 1,
            ["B"] = 2
        };

        // Act
        var result = firstDict.CompareTo(secondDict);

        // Assert
        Assert.IsEmpty(result.Added);
        Assert.IsEmpty(result.Updated);
        Assert.IsEmpty(result.Deleted);
    }
}
