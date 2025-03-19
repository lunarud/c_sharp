using NUnit.Framework;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

[TestFixture]
public class MongoChangeStreamTests
{
    private IMongoDatabase _database;
    private IMongoCollection<BsonDocument> _collection;

    [SetUp]
    public void Setup()
    {
        // Setup MongoDB connection - replace with your connection string
        var client = new MongoClient("mongodb://localhost:27017");
        _database = client.GetDatabase("test_db");
        _collection = _database.GetCollection<BsonDocument>("test_collection");
        
        // Clean up and create fresh collection
        _database.DropCollection("test_collection");
        _collection = _database.GetCollection<BsonDocument>("test_collection");
    }

    [Test]
    public async Task Test_ChangeStream_Returns_Cursor_On_Document_Insert()
    {
        // Arrange
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
            .Match(change => change.OperationType == ChangeStreamOperationType.Insert);
            
        var options = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
            BatchSize = 100
        };

        // Act
        var cursorTask = GetChangeStreamCursorAsync(pipeline, options);
        
        // Simulate an insert operation in a separate task
        var insertTask = Task.Run(async () =>
        {
            await Task.Delay(100); // Give change stream time to start
            await _collection.InsertOneAsync(new BsonDocument
            {
                { "name", "Test Document" },
                { "value", 42 }
            });
        });

        // Assert
        var cursor = await cursorTask;
        Assert.IsNotNull(cursor);

        using (cursor)
        {
            // Move to first change
            var hasChanges = await cursor.MoveNextAsync();
            Assert.IsTrue(hasChanges, "Should detect at least one change");

            var changes = cursor.Current;
            Assert.IsNotEmpty(changes, "Changes collection should not be empty");

            var firstChange = changes.First();
            Assert.AreEqual(ChangeStreamOperationType.Insert, firstChange.OperationType);
            Assert.AreEqual("Test Document", firstChange.FullDocument["name"].AsString);
            Assert.AreEqual(42, firstChange.FullDocument["value"].AsInt32);
        }

        await insertTask; // Ensure insert task completes
    }

    private Task<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>> GetChangeStreamCursorAsync(
        IPipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> pipeline,
        ChangeStreamOptions options)
    {
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
        return Task.FromResult(_collection.Watch(pipeline, options, cancellationToken));
    }

    [TearDown]
    public void TearDown()
    {
        _database.DropCollection("test_collection");
    }
}

// Example entity class (if you want to use a typed version instead of BsonDocument)
public class TestEntity
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }
}