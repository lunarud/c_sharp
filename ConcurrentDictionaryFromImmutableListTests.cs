using NUnit.Framework;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

[TestFixture]
public class ConcurrentDictionaryFromImmutableListTests
{
    private ImmutableList<KeyValuePair<string, int>> _immutableList;

    [SetUp]
    public void Setup()
    {
        _immutableList = ImmutableList.Create(
            new KeyValuePair<string, int>("One", 1),
            new KeyValuePair<string, int>("Two", 2),
            new KeyValuePair<string, int>("Three", 3)
        );
    }

    [Test]
    public void ConstructorFromImmutableList_CreatesCorrectDictionary()
    {
        // Act
        var concurrentDict = new ConcurrentDictionary<string, int>(_immutableList);

        // Assert
        Assert.AreEqual(3, concurrentDict.Count);
        Assert.IsTrue(concurrentDict.ContainsKey("One"));
        Assert.IsTrue(concurrentDict.ContainsKey("Two"));
        Assert.IsTrue(concurrentDict.ContainsKey("Three"));
        Assert.AreEqual(1, concurrentDict["One"]);
        Assert.AreEqual(2, concurrentDict["Two"]);
        Assert.AreEqual(3, concurrentDict["Three"]);
    }

    [Test]
    public void ToDictionaryConstructor_CreatesCorrectDictionary()
    {
        // Act
        var concurrentDict = new ConcurrentDictionary<string, int>(
            _immutableList.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
            )
        );

        // Assert
        Assert.AreEqual(3, concurrentDict.Count);
        Assert.IsTrue(concurrentDict.ContainsKey("One"));
        Assert.IsTrue(concurrentDict.ContainsKey("Two"));
        Assert.IsTrue(concurrentDict.ContainsKey("Three"));
        Assert.AreEqual(1, concurrentDict["One"]);
        Assert.AreEqual(2, concurrentDict["Two"]);
        Assert.AreEqual(3, concurrentDict["Three"]);
    }

    [Test]
    public void AddOrUpdateLoop_CreatesCorrectDictionary()
    {
        // Arrange
        var concurrentDict = new ConcurrentDictionary<string, int>();

        // Act
        foreach (var item in _immutableList)
        {
            concurrentDict.AddOrUpdate(item.Key, item.Value, (key, oldValue) => item.Value);
        }

        // Assert
        Assert.AreEqual(3, concurrentDict.Count);
        Assert.IsTrue(concurrentDict.ContainsKey("One"));
        Assert.IsTrue(concurrentDict.ContainsKey("Two"));
        Assert.IsTrue(concurrentDict.ContainsKey("Three"));
        Assert.AreEqual(1, concurrentDict["One"]);
        Assert.AreEqual(2, concurrentDict["Two"]);
        Assert.AreEqual(3, concurrentDict["Three"]);
    }

    [Test]
    public void AllMethods_ProduceEquivalentResults()
    {
        // Act
        var dict1 = new ConcurrentDictionary<string, int>(_immutableList);
        var dict2 = new ConcurrentDictionary<string, int>(
            _immutableList.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
            )
        );
        var dict3 = new ConcurrentDictionary<string, int>();
        foreach (var item in _immutableList)
        {
            dict3.AddOrUpdate(item.Key, item.Value, (key, oldValue) => item.Value);
        }

        // Assert
        Assert.AreEqual(dict1.Count, dict2.Count);
        Assert.AreEqual(dict2.Count, dict3.Count);

        foreach (var key in _immutableList)
        {
            Assert.IsTrue(dict1.TryGetValue(key.Key, out var value1));
            Assert.IsTrue(dict2.TryGetValue(key.Key, out var value2));
            Assert.IsTrue(dict3.TryGetValue(key.Key, out var value3));
            Assert.AreEqual(value1, value2);
            Assert.AreEqual(value2, value3);
        }
    }

    [Test]
    public void EmptyImmutableList_CreatesEmptyDictionary()
    {
        // Arrange
        var emptyList = ImmutableList<KeyValuePair<string, int>>.Empty;

        // Act
        var dict1 = new ConcurrentDictionary<string, int>(emptyList);
        var dict2 = new ConcurrentDictionary<string, int>(
            emptyList.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        );
        var dict3 = new ConcurrentDictionary<string, int>();
        foreach (var item in emptyList)
        {
            dict3.AddOrUpdate(item.Key, item.Value, (key, oldValue) => item.Value);
        }

        // Assert
        Assert.IsEmpty(dict1);
        Assert.IsEmpty(dict2);
        Assert.IsEmpty(dict3);
    }
}
