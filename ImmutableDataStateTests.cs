using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[TestFixture]
public class DataStateTests
{
    private ServiceProvider _serviceProvider;
    private IDataStateManager _stateManager;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDataStateManager, DataStateManager>();
        services.AddHostedService<DataProducerService>();
        services.AddHostedService<DataConsumerService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _stateManager = _serviceProvider.GetRequiredService<IDataStateManager>();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider.Dispose();
    }

    [Test]
    public void DataStateManager_InitialState_IsEmpty()
    {
        // Arrange & Act
        var state = _stateManager.GetCurrentState();

        // Assert
        Assert.That(state, Is.Not.Null);
        Assert.That(state, Is.Empty);
    }

    [Test]
    public void DataStateManager_UpdateState_AddsItem()
    {
        // Arrange
        var newItem = new DataItem(1, "TestValue");

        // Act
        _stateManager.UpdateState(current => current.Add(newItem));
        var state = _stateManager.GetCurrentState();

        // Assert
        Assert.That(state.Count, Is.EqualTo(1));
        Assert.That(state[0], Is.EqualTo(newItem));
    }

    [Test]
    public void DataStateManager_OnStateChanged_FiresOnUpdate()
    {
        // Arrange
        var wasCalled = false;
        ImmutableList<DataItem> receivedState = null;
        _stateManager.OnStateChanged += state => 
        { 
            wasCalled = true; 
            receivedState = state; 
        };
        var newItem = new DataItem(2, "TestValue2");

        // Act
        _stateManager.UpdateState(current => current.Add(newItem));

        // Assert
        Assert.That(wasCalled, Is.True);
        Assert.That(receivedState, Is.Not.Null);
        Assert.That(receivedState.Count, Is.EqualTo(1));
        Assert.That(receivedState[0], Is.EqualTo(newItem));
    }

    [Test]
    public async Task DataProducerService_AddsItemsOverTime()
    {
        // Arrange
        var producer = _serviceProvider.GetRequiredService<IHostedService>() as DataProducerService;
        var cts = new CancellationTokenSource();

        // Act
        await producer.StartAsync(cts.Token);
        await Task.Delay(5000); // Wait 5 seconds to allow some items to be produced
        await producer.StopAsync(cts.Token);

        // Assert
        var state = _stateManager.GetCurrentState();
        Assert.That(state.Count, Is.GreaterThan(0));
        Assert.That(state.Count, Is.LessThanOrEqualTo(3)); // Should produce 2-3 items in 5 seconds (2s interval)
    }

    [Test]
    public async Task DataConsumerService_ReceivesStateUpdates()
    {
        // Arrange
        var consumer = _serviceProvider.GetRequiredService<IHostedService>(s => s is DataConsumerService) 
            as DataConsumerService;
        var cts = new CancellationTokenSource();
        var updateReceived = new TaskCompletionSource<bool>();
        
        _stateManager.OnStateChanged += _ => updateReceived.TrySetResult(true);

        // Act
        await consumer.StartAsync(cts.Token);
        _stateManager.UpdateState(current => current.Add(new DataItem(3, "TestValue3")));
        
        // Wait for update with timeout
        var completed = await Task.WhenAny(updateReceived.Task, Task.Delay(1000));
        
        await consumer.StopAsync(cts.Token);

        // Assert
        Assert.That(completed, Is.EqualTo(updateReceived.Task), "Consumer should have received state update");
    }

    [Test]
    public void DataStateManager_ConcurrentUpdates_MaintainsConsistency()
    {
        // Arrange
        var updateCount = 100;
        var tasks = new Task[updateCount];

        // Act
        for (int i = 0; i < updateCount; i++)
        {
            var id = i;
            tasks[i] = Task.Run(() => 
                _stateManager.UpdateState(current => 
                    current.Add(new DataItem(id, $"Value-{id}"))));
        }

        Task.WaitAll(tasks);

        // Assert
        var state = _stateManager.GetCurrentState();
        Assert.That(state.Count, Is.EqualTo(updateCount));
        Assert.That(state, Is.Unique); // All items should be distinct
    }
}
