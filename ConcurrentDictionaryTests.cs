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
