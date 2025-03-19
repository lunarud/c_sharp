public class CustomChangeStreamDocument<T>
{
    public ChangeStreamOperationType OperationType { get; set; }
    public T FullDocument { get; set; }
    public BsonDocument DocumentKey { get; set; }
    public BsonTimestamp ClusterTime { get; set; }
    public CollectionNamespace CollectionNamespace { get; set; }
    public BsonDocument ResumeToken { get; set; }
    public ChangeStreamUpdateDescription UpdateDescription { get; set; }

    public CustomChangeStreamDocument(
        ChangeStreamOperationType operationType,
        T fullDocument,
        BsonDocument documentKey,
        BsonTimestamp clusterTime,
        CollectionNamespace collectionNamespace,
        BsonDocument resumeToken,
        ChangeStreamUpdateDescription updateDescription)
    {
        OperationType = operationType;
        FullDocument = fullDocument;
        DocumentKey = documentKey;
        ClusterTime = clusterTime;
        CollectionNamespace = collectionNamespace;
        ResumeToken = resumeToken;
        UpdateDescription = updateDescription;
    }
}

// Usage
var changeDocument = new CustomChangeStreamDocument<MyDocument>(
    operationType: ChangeStreamOperationType.Insert,
    fullDocument: new MyDocument { Id = "1", Name = "Test" },
    documentKey: new BsonDocument("_id", "1"),
    clusterTime: new BsonTimestamp(1),
    collectionNamespace: new CollectionNamespace("test", "mydocs"),
    resumeToken: new BsonDocument("token", "resume"),
    updateDescription: null);

public class MyDocument
{
    public string Id { get; set; }
    public string Name { get; set; }
}
using MongoDB.Driver;
using MongoDB.Bson;

var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("test");
var collection = database.GetCollection<MyDocument>("mydocs");

var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<MyDocument>>()
    .Match("{ operationType: 'insert' }");

using (var cursor = collection.Watch(pipeline))
{
    while (cursor.MoveNext())
    {
        foreach (var change in cursor.Current)
        {
            Console.WriteLine($"Operation: {change.OperationType}");
            Console.WriteLine($"Document: {change.FullDocument.Name}");
        }
    }
}

public class MyDocument
{
    public string Id { get; set; }
    public string Name { get; set; }
}

using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

public interface IMongoChangeStreamService
{
    Task<IChangeStreamCursor<ChangeStreamDocument<T>>> WatchAsync<T>(
        IMongoCollection<T> collection,
        CancellationToken cancellationToken);
}



public class MongoChangeStreamService : IMongoChangeStreamService
{
    public async Task<IChangeStreamCursor<ChangeStreamDocument<T>>> WatchAsync<T>(
        IMongoCollection<T> collection,
        CancellationToken cancellationToken)
    {
        var options = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
        };

        // Use WatchAsync to start watching the collection
        return await collection.WatchAsync(
            pipeline: new EmptyPipelineDefinition<ChangeStreamDocument<T>>(),
            options: options,
            cancellationToken: cancellationToken);
    }
}


public class ChangeStreamConsumer
{
    private readonly IMongoChangeStreamService _changeStreamService;
    private readonly IMongoCollection<MyDocument> _collection;

    public ChangeStreamConsumer(IMongoChangeStreamService changeStreamService, IMongoCollection<MyDocument> collection)
    {
        _changeStreamService = changeStreamService;
        _collection = collection;
    }

    public async Task ProcessChangeStreamAsync(CancellationToken cancellationToken)
    {
        using var cursor = await _changeStreamService.WatchAsync(_collection, cancellationToken);
        await cursor.ForEachAsync(change =>
        {
            // Process the change document (e.g., log it, update state, etc.)
            Console.WriteLine($"Change detected: {change.OperationType}, Document: {change.FullDocument}");
        }, cancellationToken);
    }
}

// Example document class
public class MyDocument
{
    public string Id { get; set; }
    public string Name { get; set; }
}


using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

[TestClass]
public class ChangeStreamConsumerTests
{
    [TestMethod]
    public async Task ProcessChangeStreamAsync_HandlesInsertOperation()
    {
        // Arrange
        var mockChangeStreamService = new Mock<IMongoChangeStreamService>();
        var mockCollection = new Mock<IMongoCollection<MyDocument>>();

        // Mock the change stream cursor
        var mockCursor = new Mock<IChangeStreamCursor<ChangeStreamDocument<MyDocument>>>();
        var changeDocument = new ChangeStreamDocument<MyDocument>(
            operationType: ChangeStreamOperationType.Insert,
            fullDocument: new MyDocument { Id = "1", Name = "Test" },
            documentKey: new BsonDocument("_id", "1"),
            clusterTime: new BsonTimestamp(1),
            collectionNamespace: new CollectionNamespace("test", "mydocs"),
            resumeToken: new BsonDocument("token", "resume"),
            updateDescription: null);

        // Simulate the ForEachAsync behavior by setting up an async enumerable
        var changes = new[] { changeDocument }.ToAsyncEnumerable();
        mockCursor.Setup(c => c.ToEnumerable(It.IsAny<CancellationToken>())).Returns(changes);

        mockChangeStreamService
            .Setup(s => s.WatchAsync(mockCollection.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        var consumer = new ChangeStreamConsumer(mockChangeStreamService.Object, mockCollection.Object);

        // Act
        await consumer.ProcessChangeStreamAsync(CancellationToken.None);

        // Assert
        mockCursor.Verify(c => c.ToEnumerable(It.IsAny<CancellationToken>()), Times.Once());
        // Add additional assertions based on what ProcessChangeStreamAsync does with the change
    }
}

// Helper to convert array to IAsyncEnumerable
public static class EnumerableExtensions
{
    public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        return new AsyncEnumerableWrapper<T>(source);
    }

    private class AsyncEnumerableWrapper<T> : IAsyncEnumerable<T>
    {
        private readonly IEnumerable<T> _source;

        public AsyncEnumerableWrapper(IEnumerable<T> source)
        {
            _source = source;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncEnumeratorWrapper<T>(_source.GetEnumerator());
        }
    }

    private class AsyncEnumeratorWrapper<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;

        public AsyncEnumeratorWrapper(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public T Current => _enumerator.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_enumerator.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            _enumerator.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
