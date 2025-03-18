https://github.com/astorDev/persic/blob/main/mongo/dotnet/playground/ChangeListening.cs
https://github.com/mongodb/mongo-csharp-driver/blob/main/tests/MongoDB.Driver.Examples/ChangeStreamExamples.cs

using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;

// Assuming this is your service class that uses ChangeStream
public class MongoChangeStreamService
{
    private readonly IMongoCollection<MyDocument> _collection;

    public MongoChangeStreamService(IMongoCollection<MyDocument> collection)
    {
        _collection = collection;
    }

    public async Task<string> WatchChangesAsync(CancellationToken cancellationToken)
    {
        var pipeline = PipelineDefinition<MyDocument, ChangeStreamDocument<MyDocument>>.Create(new EmptyPipelineDefinition<ChangeStreamDocument<MyDocument>>());
        var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };

        using var cursor = await _collection.WatchAsync(pipeline, options, cancellationToken);
        await cursor.MoveNextAsync(cancellationToken);
        
        if (!cursor.Current.Any())
            return "No changes";

        return cursor.Current.First().OperationType.ToString();
    }
}

// Document class
public class MyDocument
{
    public string Id { get; set; }
    public string Name { get; set; }
}

// Unit tests
[TestFixture]
public class MongoChangeStreamServiceTests
{
    private Mock<IMongoCollection<MyDocument>> _mockCollection;
    private Mock<IAsyncCursor<ChangeStreamDocument<MyDocument>>> _mockCursor;
    private MongoChangeStreamService _service;

    [SetUp]
    public void Setup()
    {
        _mockCollection = new Mock<IMongoCollection<MyDocument>>();
        _mockCursor = new Mock<IAsyncCursor<ChangeStreamDocument<MyDocument>>>();
        _service = new MongoChangeStreamService(_mockCollection.Object);
    }

    [Test]
    public async Task WatchChangesAsync_WhenChangesExist_ReturnsOperationType()
    {
        // Arrange
        var changeDoc = new ChangeStreamDocument<MyDocument>(
            operationType: ChangeStreamOperationType.Insert,
            fullDocument: new MyDocument { Id = "1", Name = "Test" },
            ns: new ChangeStreamNamespace("testDb", "testCollection"),
            documentKey: null,
            updateDescription: null,
            resumeToken: null,
            clusterTime: null,
            txnNumber: null,
            lsid: null);

        _mockCursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _mockCursor.Setup(c => c.Current)
            .Returns(new[] { changeDoc });

        _mockCursor.Setup(c => c.Dispose());

        _mockCollection.Setup(c => c.WatchAsync(
            It.IsAny<PipelineDefinition<MyDocument, ChangeStreamDocument<MyDocument>>>(),
            It.IsAny<ChangeStreamOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockCursor.Object);

        // Act
        var result = await _service.WatchChangesAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual("Insert", result);
    }

    [Test]
    public async Task WatchChangesAsync_WhenNoChanges_ReturnsNoChanges()
    {
        // Arrange
        _mockCursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _mockCursor.Setup(c => c.Current)
            .Returns(new ChangeStreamDocument<MyDocument>[0]);

        _mockCursor.Setup(c => c.Dispose());

        _mockCollection.Setup(c => c.WatchAsync(
            It.IsAny<PipelineDefinition<MyDocument, ChangeStreamDocument<MyDocument>>>(),
            It.IsAny<ChangeStreamOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockCursor.Object);

        // Act
        var result = await _service.WatchChangesAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual("No changes", result);
    }

    [Test]
    public async Task WatchChangesAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockCursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        _mockCollection.Setup(c => c.WatchAsync(
            It.IsAny<PipelineDefinition<MyDocument, ChangeStreamDocument<MyDocument>>>(),
            It.IsAny<ChangeStreamOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockCursor.Object);

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _service.WatchChangesAsync(cts.Token));
    }
}
