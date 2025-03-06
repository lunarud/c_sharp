using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

// Domain Models (Immutable)
public record DataItem(int Id, string Value);

// Shared State Management
public interface IDataStateManager
{
    ImmutableList<DataItem> GetCurrentState();
    void UpdateState(Func<ImmutableList<DataItem>, ImmutableList<DataItem>> updateFunc);
    event Action<ImmutableList<DataItem>> OnStateChanged;
}

public class DataStateManager : IDataStateManager
{
    private ImmutableList<DataItem> _currentState = ImmutableList<DataItem>.Empty;
    public event Action<ImmutableList<DataItem>> OnStateChanged = _ => { };

    public ImmutableList<DataItem> GetCurrentState() => _currentState;

    public void UpdateState(Func<ImmutableList<DataItem>, ImmutableList<DataItem>> updateFunc)
    {
        lock (this)
        {
            var newState = updateFunc(_currentState);
            _currentState = newState;
            OnStateChanged(newState);
        }
    }
}

// Service 1: Data Producer
public class DataProducerService : BackgroundService
{
    private readonly IDataStateManager _stateManager;

    public DataProducerService(IDataStateManager stateManager)
    {
        _stateManager = stateManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var random = new Random();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(2000, stoppingToken); // Wait 2 seconds
            
            _stateManager.UpdateState(current => 
                current.Add(new DataItem(
                    random.Next(1, 1000),
                    $"Value-{DateTime.Now.Ticks}"
                ))
            );

            Console.WriteLine($"Producer: Added new item. List size: {_stateManager.GetCurrentState().Count}");
        }
    }
}

// Service 2: Data Consumer
public class DataConsumerService : BackgroundService
{
    private readonly IDataStateManager _stateManager;

    public DataConsumerService(IDataStateManager stateManager)
    {
        _stateManager = stateManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Pure function to process the state
        static string ProcessState(ImmutableList<DataItem> items) =>
            $"Consumer received {items.Count} items. Latest: {(items.Count > 0 ? items[^1].ToString() : "None")}";

        _stateManager.OnStateChanged += state =>
        {
            Console.WriteLine(ProcessState(state));
        };

        // Keep service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

// Program Setup
class Program
{
    static Task Main(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<IDataStateManager, DataStateManager>();
                services.AddHostedService<DataProducerService>();
                services.AddHostedService<DataConsumerService>();
            })
            .Build()
            .RunAsync();
}