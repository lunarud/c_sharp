using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        // Example dictionaries
        var dictionary1 = new ConcurrentDictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
            ["key3"] = "value3"
        };

        var dictionary2 = new ConcurrentDictionary<string, string>
        {
            ["key2"] = "value2", // Same value
            ["key3"] = "updatedValue3", // Updated value
            ["key4"] = "value4" // New key-value pair
        };

        // Compare dictionaries
        var result = CompareDictionaries(dictionary1, dictionary2);

        // Output results
        Console.WriteLine("Added:");
        foreach (var kvp in result.Added)
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");

        Console.WriteLine("\nUpdated:");
        foreach (var kvp in result.Updated)
            Console.WriteLine($"{kvp.Key}: {kvp.OldValue} -> {kvp.NewValue}");

        Console.WriteLine("\nDeleted:");
        foreach (var kvp in result.Deleted)
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    }

    static ComparisonResult<string, string> CompareDictionaries<TKey, TValue>(
        ConcurrentDictionary<TKey, TValue> dict1,
        ConcurrentDictionary<TKey, TValue> dict2)
    {
        var added = new Dictionary<TKey, TValue>();
        var updated = new Dictionary<TKey, (TValue OldValue, TValue NewValue)>();
        var deleted = new Dictionary<TKey, TValue>();

        // Find added and updated keys
        foreach (var kvp in dict2)
        {
            if (!dict1.TryGetValue(kvp.Key, out var oldValue))
            {
                // Key exists in dict2 but not in dict1 (Added)
                added[kvp.Key] = kvp.Value;
            }
            else if (!EqualityComparer<TValue>.Default.Equals(oldValue, kvp.Value))
            {
                // Key exists in both but values differ (Updated)
                updated[kvp.Key] = (OldValue: oldValue, NewValue: kvp.Value);
            }
        }

        // Find deleted keys
        foreach (var kvp in dict1)
        {
            if (!dict2.ContainsKey(kvp.Key))
            {
                // Key exists in dict1 but not in dict2 (Deleted)
                deleted[kvp.Key] = kvp.Value;
            }
        }

        return new ComparisonResult<TKey, TValue>
        {
            Added = added,
            Updated = updated,
            Deleted = deleted
        };
    }
}

// Result class to store comparison results
class ComparisonResult<TKey, TValue>
{
    public Dictionary<TKey, TValue> Added { get; set; } = new();
    public Dictionary<TKey, (TValue OldValue, TValue NewValue)> Updated { get; set; } = new();
    public Dictionary<TKey, TValue> Deleted { get; set; } = new();
}
