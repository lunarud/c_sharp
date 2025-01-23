using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Observable service for event-driven communication
public class EventService : IObservable<string>
{
    private readonly Subject<string> _subject = new();

    public IDisposable Subscribe(IObserver<string> observer)
    {
        return _subject.Subscribe(observer);
    }

    public void Publish(string message)
    {
        _subject.OnNext(message);
    }
}

// Singleton holding a ConcurrentDictionary
public class DataStore
{
    private readonly ConcurrentDictionary<string, string> _dictionary = new();

    public void AddOrUpdate(string key, string value)
    {
        _dictionary.AddOrUpdate(key, value, (_, _) => value);
        Console.WriteLine($"DataStore updated: [{key}] = {value}");
    }
}

// Observer for the EventService
public class EventObserver : IObserver<string>
{
    private readonly DataStore _dataStore;

    public EventObserver(DataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public void OnNext(string value)
    {
        Console.WriteLine($"EventObserver received: {value}");
        _dataStore.AddOrUpdate(value, DateTime.UtcNow.ToString());
    }

    public void OnError(Exception error)
    {
        Console.WriteLine($"Error: {error.Message}");
    }

    public void OnCompleted()
    {
        Console.WriteLine("EventObserver: Completed");
    }
}

// HostedService publishing events
public class PublisherHostedService : IHostedService
{
    private readonly EventService _eventService;

    public PublisherHostedService(EventService eventService)
    {
        _eventService = eventService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("PublisherHostedService started.");

        Task.Run(async () =>
        {
            for (int i = 0; i < 5; i++)
            {
                _eventService.Publish($"Message {i}");
                await Task.Delay(1000, cancellationToken);
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("PublisherHostedService stopped.");
        return Task.CompletedTask;
    }
}

// BackgroundService consuming the events
public class ConsumerBackgroundService : BackgroundService
{
    private readonly EventService _eventService;
    private IDisposable _subscription;

    public ConsumerBackgroundService(EventService eventService, DataStore dataStore)
    {
        _eventService = eventService;
        var observer = new EventObserver(dataStore);
        _subscription = _eventService.Subscribe(observer);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("ConsumerBackgroundService is running.");
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _subscription.Dispose();
        base.Dispose();
    }
}

// Program
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<EventService>();
                services.AddSingleton<DataStore>();
                services.AddHostedService<PublisherHostedService>();
                services.AddHostedService<ConsumerBackgroundService>();
            })
            .Build();

        await host.RunAsync();
    }
}
