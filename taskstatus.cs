using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class LazyAsyncFunctionStore
{
    private class TaskInfo
    {
        public Func<Task> Function { get; }
        public SemaphoreSlim Lock { get; } = new(1, 1); // Prevent multiple execution

        public TaskInfo(Func<Task> function)
        {
            Function = function;
        }
    }

    private readonly Lazy<Task<ConcurrentDictionary<string, TaskInfo>>> _lazyFunctions;

    public LazyAsyncFunctionStore()
    {
        _lazyFunctions = new Lazy<Task<ConcurrentDictionary<string, TaskInfo>>>(InitializeAsync);
    }

    private async Task<ConcurrentDictionary<string, TaskInfo>> InitializeAsync()
    {
        await Task.Yield(); // Simulating async initialization
        return new ConcurrentDictionary<string, TaskInfo>();
    }

    // Add a function safely
    public async Task<bool> AddFunctionAsync(string key, Func<Task> function)
    {
        if (string.IsNullOrWhiteSpace(key) || function is null)
            throw new ArgumentNullException(nameof(key), "Key or function cannot be null");

        var dictionary = await _lazyFunctions.Value.ConfigureAwait(false);
        return dictionary.TryAdd(key, new TaskInfo(function));
    }

    // Attempt to remove a function, but only if it's not running
    public async Task<bool> RemoveFunctionAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty");

        var dictionary = await _lazyFunctions.Value.ConfigureAwait(false);

        if (dictionary.TryGetValue(key, out var taskInfo))
        {
            // Ensure the function is not running
            if (await taskInfo.Lock.WaitAsync(0))
            {
                try
                {
                    return dictionary.TryRemove(key, out _);
                }
                finally
                {
                    taskInfo.Lock.Release();
                }
            }
            else
            {
                Console.WriteLine($"Cannot remove {key} as it is currently running.");
                return false;
            }
        }

        return false;
    }

    // Execute a function asynchronously
    public async Task ExecuteFunctionAsync(string key)
    {
        var dictionary = await _lazyFunctions.Value.ConfigureAwait(false);

        if (dictionary.TryGetValue(key, out var taskInfo))
        {
            if (await taskInfo.Lock.WaitAsync(0)) // Prevent multiple executions
            {
                try
                {
                    await taskInfo.Function();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing function {key}: {ex.Message}");
                }
                finally
                {
                    taskInfo.Lock.Release();
                }
            }
            else
            {
                Console.WriteLine($"Task {key} is already running.");
            }
        }
        else
        {
            Console.WriteLine($"Function with key '{key}' not found.");
        }
    }
}