using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using NUnit.Framework;

[TestFixture]
public class MongoChangeStreamServiceTests
{
    private Mock<ILogger<MongoChangeStreamService>> _loggerMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<IAsyncCursor<ChangeStreamDocument<ChangeRecord>>> _cursorMock;
    private CancellationTokenSource _cancellationTokenSource;
    private MongoChangeStreamService _service;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<MongoChangeStreamService>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cursorMock = new Mock<IAsyncCursor<ChangeStreamDocument<ChangeRecord>>>();
        _cancellationTokenSource = new CancellationTokenSource();

        _service = new MongoChangeStreamService(
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task ChangeStreamExecute_LogsStartAndStopMessages()
    {
        // Arrange
        var cancellationToken = _cancellationTokenSource.Token;

        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<ChangeRecord>>();
        var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };

        var changes = new List<ChangeStreamDocument<ChangeRecord>>
        {
            new ChangeStreamDocument<ChangeRecord>
            {
                FullDocument = new ChangeRecord() // Mock a document
            }
        };

        _cursorMock.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)  // First call returns data
            .Returns(false); // Second call ends the loop

        _cursorMock.SetupGet(c => c.Current).Returns(changes);

        _unitOfWorkMock
            .Setup(u => u.Collection.WatchAsync(pipeline, options, cancellationToken))
            .ReturnsAsync(_cursorMock.Object);

        // Act
        await _service.ChangeStreamExecute(cancellationToken);

        // Assert
        _loggerMock.Verify(logger => logger.LogInformation("MongoChangeStreamService is starting."), Times.Once);
        _loggerMock.Verify(logger => logger.LogInformation("MongoChangeStreamService is now watching for changes."), Times.Once);
        _loggerMock.Verify(logger => logger.LogInformation("MongoChangeStreamService is stopping."), Times.Once);
    }

    [Test]
    public async Task ChangeStreamExecute_CatchesAndLogsExceptions()
    {
        // Arrange
        var cancellationToken = _cancellationTokenSource.Token;

        _unitOfWorkMock
            .Setup(u => u.Collection.WatchAsync(It.IsAny<PipelineDefinition<ChangeStreamDocument<ChangeRecord>, ChangeStreamDocument<ChangeRecord>>>(), 
                                                It.IsAny<ChangeStreamOptions>(), 
                                                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _service.ChangeStreamExecute(cancellationToken);

        // Assert
        _loggerMock.Verify(logger => logger.LogError(It.IsAny<Exception>(), "An error occurred in MongoChangeStreamService."), Times.Once);
    }

    [TearDown]
    public void Teardown()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
}
