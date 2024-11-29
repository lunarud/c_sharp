https://github.com/productinfo/mongo-csharp-driver/blob/7c9596b35631f5c7b269b242a9e4ae723a3be687/tests/MongoDB.Driver.Core.Tests/ChangeStreamDocumentTests.cs#L35

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;
using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Logging;

[TestFixture]
public class ChangeStreamExecuteTests
{
    private Mock<ILogger<YourClass>> _loggerMock;
    private Mock<IApiUnitOfWork> _apiUnitOfWorkMock;
    private Mock<IChangeRecordsRepo> _changeRecordsRepoMock;
    private Mock<IPipelineStages> _pipelineStagesMock;

    private YourClass _service; // Replace 'YourClass' with the class containing ChangeStreamExecute

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<YourClass>>();
        _apiUnitOfWorkMock = new Mock<IApiUnitOfWork>();
        _changeRecordsRepoMock = new Mock<IChangeRecordsRepo>();
        _pipelineStagesMock = new Mock<IPipelineStages>();

        _apiUnitOfWorkMock.SetupGet(x => x.ChangeRecordsRepo).Returns(_changeRecordsRepoMock.Object);

        // Create the service class
        _service = new YourClass(_apiUnitOfWorkMock.Object, _loggerMock.Object, _pipelineStagesMock.Object);
    }

    [Test]
    public async Task ChangeStreamExecute_ShouldLogInformationAndProcessInsertOperations()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;

        var mockCursor = new Mock<IAsyncCursor<ChangeStreamDocument<ChangeRecord>>>();
        var changeStreamDocument = new ChangeStreamDocument<ChangeRecord>(
            new BsonDocument(), 
            ChangeStreamOperationType.Insert, 
            default, 
            default, 
            default, 
            new ChangeRecord() // Assume ChangeRecord is a valid class
        );

        mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true) // First iteration
                  .ReturnsAsync(false); // End of stream

        mockCursor.Setup(x => x.Current).Returns(new List<ChangeStreamDocument<ChangeRecord>> { changeStreamDocument });

        _changeRecordsRepoMock.Setup(x => x.Watch(
            It.IsAny<EmptyPipelineDefinition<ChangeStreamDocument<ChangeRecord>>>(),
            It.IsAny<ChangeStreamOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(mockCursor.Object);

        _pipelineStagesMock.Setup(x => x.ChangeRecordToFlatRecord(
            It.IsAny<IApiUnitOfWork>(),
            It.IsAny<ChangeRecord>()))
            .ReturnsAsync(new List<FlatRecord> { new FlatRecord() }); // Assume FlatRecord is a valid class

        // Act
        await _service.ChangeStreamExecute(cancellationToken);

        // Assert
        _loggerMock.Verify(x => x.LogInformation("Subscribe to Change Stream"), Times.Once);
        _changeRecordsRepoMock.Verify(x => x.Watch(
            It.IsAny<EmptyPipelineDefinition<ChangeStreamDocument<ChangeRecord>>>(),
            It.IsAny<ChangeStreamOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _pipelineStagesMock.Verify(x => x.ChangeRecordToFlatRecord(
            It.IsAny<IApiUnitOfWork>(),
            It.IsAny<ChangeRecord>()), Times.Once);
    }

    [Test]
    public void ChangeStreamExecute_ShouldCancelExecution_WhenCancellationRequested()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () => 
            await _service.ChangeStreamExecute(cancellationTokenSource.Token));
    }
}
