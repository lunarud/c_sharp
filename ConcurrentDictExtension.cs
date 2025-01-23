using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public static class EnumerableExtensions
{
    /// <summary>
    /// Converts an IEnumerable to a ConcurrentDictionary.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source collection.</typeparam>
    /// <typeparam name="TKey">The type of the key in the resulting dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the value in the resulting dictionary.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="keySelector">A function to extract the key from each element.</param>
    /// <param name="valueSelector">A function to extract the value from each element.</param>
    /// <returns>A ConcurrentDictionary containing the keys and values.</returns>
    public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TSource, TKey, TValue>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));

        var dictionary = new ConcurrentDictionary<TKey, TValue>();

        foreach (var item in source)
        {
            dictionary.TryAdd(keySelector(item), valueSelector(item));
        }

        return dictionary;
    }

    /// <summary>
    /// Converts an IEnumerable to a ConcurrentDictionary with the default value selector.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source collection.</typeparam>
    /// <typeparam name="TKey">The type of the key in the resulting dictionary.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="keySelector">A function to extract the key from each element.</param>
    /// <returns>A ConcurrentDictionary containing the keys and the elements as values.</returns>
    public static ConcurrentDictionary<TKey, TSource> ToConcurrentDictionary<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return source.ToConcurrentDictionary(keySelector, item => item);
    }
}