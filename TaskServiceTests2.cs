using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class TaskServiceTests
{
    private TaskService _taskService;

    [SetUp]
    public void Setup()
    {
        _taskService = new TaskService();
    }

    [Test]
    public async Task PerformOperationsAsync_WhenTaskIsCanceled_ShouldCatchOperationCanceledException()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var tasks = new List<Task>
        {
            Task.Delay(100), // Normal task
            Task.Run(() => throw new OperationCanceledException(cancellationTokenSource.Token)) // Canceled task
        };

        // Act
        Assert.DoesNotThrowAsync(async () => await _taskService.PerformOperationsAsync(tasks));

        // Since the exception is caught inside the service method, we expect no exception to propagate here
    }

    [Test]
    public async Task PerformOperationsAsync_WhenNoTaskIsCanceled_ShouldNotThrowException()
    {
        // Arrange
        var tasks = new List<Task>
        {
            Task.Delay(100), // Normal task
            Task.Delay(200)  // Another normal task
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _taskService.PerformOperationsAsync(tasks));
    }
}
