public class ConsumerBackgroundService : BackgroundService
{
    private readonly DataStore _dataStore;
    private readonly EventService _eventService;
    private IDisposable _subscription;

    public ConsumerBackgroundService(EventService eventService, DataStore dataStore)
    {
        _dataStore = dataStore;
        _eventService = eventService;
        var observer = new EventObserver(dataStore);
        _subscription = _eventService.Subscribe(observer);

        // Refactored: Pointing to the method
        _dataStore.OnDataUpdated += HandleDataUpdated;
    }

    // New method to handle the event
    private void HandleDataUpdated(string action, string key)
    {
        Console.WriteLine($"ConsumerBackgroundService received notification: {action} operation on key '{key}'");
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
