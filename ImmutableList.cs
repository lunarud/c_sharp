using System.Collections.Concurrent;
using System.Collections.Immutable;

// Assuming you have an ImmutableList
ImmutableList<KeyValuePair<string, int>> immutableList = ImmutableList.Create(
    new KeyValuePair<string, int>("One", 1),
    new KeyValuePair<string, int>("Two", 2),
    new KeyValuePair<string, int>("Three", 3)
);

// Method 1: Using constructor
ConcurrentDictionary<string, int> concurrentDict1 = 
    new ConcurrentDictionary<string, int>(immutableList);

// Method 2: Using ToDictionary and then constructor
ConcurrentDictionary<string, int> concurrentDict2 = 
    new ConcurrentDictionary<string, int>(immutableList.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value
    ));

// Method 3: Using AddOrUpdate in a loop
ConcurrentDictionary<string, int> concurrentDict3 = new ConcurrentDictionary<string, int>();
foreach (var item in immutableList)
{
    concurrentDict3.AddOrUpdate(item.Key, item.Value, (key, oldValue) => item.Value);
}