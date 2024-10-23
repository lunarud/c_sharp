using NUnit.Framework;
using System;
using System.Collections.Generic;
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
    public async Task PerformOperationsAsync_WhenAllTasksCompleteSuccessfully_ShouldNotThrowException()
    {
        // Arrange
        var tasks = new List<Task>
        {
            Task.Delay(100),
            Task.Delay(200)
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _taskService.PerformOperationsAsync(tasks));
    }

    [Test]
    public async Task PerformOperationsAsync_WhenOneTaskFails_ShouldThrowAggregateException()
    {
        // Arrange
        var tasks = new List<Task>
        {
            Task.Delay(100),
            Task.FromException(new InvalidOperationException("Task failed"))
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<AggregateException>(async () => await _taskService.PerformOperationsAsync(tasks));

        // Verify that the exception contains the expected exception type and message
        Assert.That(ex.InnerExceptions[0], Is.TypeOf<InvalidOperationException>());
        Assert.That(ex.InnerExceptions[0].Message, Is.EqualTo("Task failed"));
    }

    [Test]
    public async Task PerformOperationsAsync_WhenMultipleTasksFail_ShouldThrowAggregateException()
    {
        // Arrange
        var tasks = new List<Task>
        {
            Task.FromException(new InvalidOperationException("First task failed")),
            Task.FromException(new ArgumentException("Second task failed"))
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<AggregateException>(async () => await _taskService.PerformOperationsAsync(tasks));

        // Verify that the exception contains both exceptions
        Assert.That(ex.InnerExceptions.Count, Is.EqualTo(2));
        Assert.That(ex.InnerExceptions[0], Is.TypeOf<InvalidOperationException>());
        Assert.That(ex.InnerExceptions[0].Message, Is.EqualTo("First task failed"));
        Assert.That(ex.InnerExceptions[1], Is.TypeOf<ArgumentException>());
        Assert.That(ex.InnerExceptions[1].Message, Is.EqualTo("Second task failed"));
    }
}
