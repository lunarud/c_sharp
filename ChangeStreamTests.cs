using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Collections.Generic;

[TestFixture]
public class ChangeStreamExecuteTests
{
    private Mock<ILogger> _mockLogger;
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IChangeRecordsRepo> _mockChangeRecordsRepo;
    private Mock<IChangeRecordsFlatRepo> _mockChangeRecordsFlatRepo;
    private CancellationTokenSource _cancellationTokenSource;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockChangeRecordsRepo = new Mock<IChangeRecordsRepo>();
        _mockChangeRecordsFlatRepo = new Mock<IChangeRecordsFlatRepo>();
        _cancellationTokenSource = new CancellationTokenSource();

        _mockUnitOfWork.Setup(u => u.ChangeRecordsRepo).Returns(_mockChangeRecordsRepo.Object);
        _mockUnitOfWork.Setup(u => u.ChangeRecordsFlatRepo).Returns(_mockChangeRecordsFlatRepo.Object);
    }

    [Test]
    public async Task ChangeStreamExecute_ProcessesChangeStreamCorrectly()
    {
        // Arrange
        var changeStreamOptions = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
        };

        var mockCursor = new Mock<IAsyncCursor<ChangeStreamDocument<ChangeRecord>>>();
        var mockChangeRecord = new ChangeRecord { /* populate fields for testing */ };
        var changeStreamDocument = new ChangeStreamDocument<ChangeRecord>
        {
            FullDocument = mockChangeRecord,
            OperationType = ChangeStreamOperationType.Insert
        };

        mockCursor
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        mockCursor.Setup(c => c.Current).Returns(new[] { changeStreamDocument });

        _mockChangeRecordsRepo
            .Setup(r => r.Watch(It.IsAny<PipelineDefinition<ChangeStreamDocument<ChangeRecord>, ChangeStreamDocument<ChangeRecord>>>(), changeStreamOptions, It.IsAny<CancellationToken>()))
            .Returns(mockCursor.Object);

        var pipelineStages = new Mock<PipelineStages>(_mockUnitOfWork.Object);
        pipelineStages
            .Setup(p => p.ChangeRecordToFlatRecord(It.IsAny<ChangeRecord>()))
            .ReturnsAsync(new List<BsonDocument> { new BsonDocument { { "timestamp", DateTime.UtcNow } } });

        _mockUnitOfWork.Setup(u => u.ChangeRecordsFlatRepo.CreateAsync(It.IsAny<ChangeRecordsFlat>()))
            .Returns(Task.CompletedTask);

        var service = new MyService(_mockLogger.Object, _mockUnitOfWork.Object, pipelineStages.Object);

        // Act
        await service.ChangeStreamExecute(_cancellationTokenSource.Token);

        // Assert
        _mockLogger.Verify(l => l.LogInformation("Subscribe to Change Stream"), Times.Once);
        _mockUnitOfWork.Verify(u => u.ChangeRecordsFlatRepo.CreateAsync(It.IsAny<ChangeRecordsFlat>()), Times.Once);
    }
}
