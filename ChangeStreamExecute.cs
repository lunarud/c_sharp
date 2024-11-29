[Test]
public async Task ChangeStreamExecute_ProcessesChangeStreamCorrectly()
{
    // Arrange
    var changeStreamOptions = new ChangeStreamOptions
    {
        FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
    };

    var mockCursor = new Mock<IAsyncCursor<ChangeStreamDocument<BsonDocument>>>();

    // Simulate a ChangeStreamDocument with mocked properties
    var bsonDocument = new BsonDocument
    {
        { "timestamp", DateTime.UtcNow }
    };

    var changeStreamDocument = new ChangeStreamDocument<BsonDocument>(
        fullDocument: bsonDocument,
        namespaceDocument: null,
        documentKey: null,
        updateDescription: null,
        clusterTime: null,
        operationType: ChangeStreamOperationType.Insert,
        resumeToken: BsonDocument.Parse("{}")
    );

    mockCursor
        .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(true)
        .ReturnsAsync(false);

    mockCursor.Setup(c => c.Current).Returns(new[] { changeStreamDocument });

    _mockChangeRecordsRepo
        .Setup(r => r.Watch(It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(), changeStreamOptions, It.IsAny<CancellationToken>()))
        .Returns(mockCursor.Object);

    var pipelineStages = new Mock<PipelineStages>(_mockUnitOfWork.Object);
    pipelineStages
        .Setup(p => p.ChangeRecordToFlatRecord(It.IsAny<BsonDocument>()))
        .ReturnsAsync(new List<BsonDocument> { bsonDocument });

    _mockUnitOfWork.Setup(u => u.ChangeRecordsFlatRepo.CreateAsync(It.IsAny<ChangeRecordsFlat>()))
        .Returns(Task.CompletedTask);

    var service = new MyService(_mockLogger.Object, _mockUnitOfWork.Object, pipelineStages.Object);

    // Act
    await service.ChangeStreamExecute(_cancellationTokenSource.Token);

    // Assert
    _mockLogger.Verify(l => l.LogInformation("Subscribe to Change Stream"), Times.Once);
    _mockUnitOfWork.Verify(u => u.ChangeRecordsFlatRepo.CreateAsync(It.IsAny<ChangeRecordsFlat>()), Times.Once);
}
