using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public static class ConcurrentDictionaryExtensions
{
    public static ComparisonResult<TKey, TValue> CompareTo<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> firstDictionary,
        ConcurrentDictionary<TKey, TValue> secondDictionary)
    {
        var added = new Dictionary<TKey, TValue>();
        var updated = new Dictionary<TKey, (TValue OldValue, TValue NewValue)>();
        var deleted = new Dictionary<TKey, TValue>();

        // Find added and updated keys
        foreach (var kvp in secondDictionary)
        {
            if (!firstDictionary.TryGetValue(kvp.Key, out var oldValue))
            {
                // Key exists in secondDictionary but not in firstDictionary (Added)
                added[kvp.Key] = kvp.Value;
            }
            else if (!EqualityComparer<TValue>.Default.Equals(oldValue, kvp.Value))
            {
                // Key exists in both but values differ (Updated)
                updated[kvp.Key] = (OldValue: oldValue, NewValue: kvp.Value);
            }
        }

        // Find deleted keys
        foreach (var kvp in firstDictionary)
        {
            if (!secondDictionary.ContainsKey(kvp.Key))
            {
                // Key exists in firstDictionary but not in secondDictionary (Deleted)
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
public class ComparisonResult<TKey, TValue>
{
    public Dictionary<TKey, TValue> Added { get; set; } = new();
   
