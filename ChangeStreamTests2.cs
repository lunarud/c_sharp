using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;
using Moq;
using NUnit.Framework;

[TestFixture]
public class ChangeStreamTests
{
    private Mock<IMongoClient> _mockMongoClient;
    private Mock<IMongoDatabase> _mockDatabase;
    private Mock<IMongoCollection<BsonDocument>> _mockCollection;
    private Mock<IChangeStreamCursor<BsonDocument>> _mockCursor;

    [SetUp]
    public void Setup()
    {
        _mockMongoClient = new Mock<IMongoClient>();
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<BsonDocument>>();
        _mockCursor = new Mock<IChangeStreamCursor<BsonDocument>>();

        // Setup the MongoClient to return mocked database and collection
        _mockMongoClient
            .Setup(client => client.GetDatabase(It.IsAny<string>(), null))
            .Returns(_mockDatabase.Object);

        _mockDatabase
            .Setup(db => db.GetCollection<BsonDocument>(It.IsAny<string>(), null))
            .Returns(_mockCollection.Object);
    }

    [Test]
    public void ChangeStream_ShouldInitializeCorrectly()
    {
        // Arrange
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
            .Match(change =>
                change.OperationType == ChangeStreamOperationType.Insert ||
                change.OperationType == ChangeStreamOperationType.Update ||
                change.OperationType == ChangeStreamOperationType.Replace
            )
            .AppendStage<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>, BsonDocument>(
                @"{ 
                    $project: { 
                        '_id': 1, 
                        'fullDocument': 1, 
                        'ns': 1, 
                        'documentKey': 1 
                    }
                }"
            );

        ChangeStreamOptions options = new()
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
        };

        // Mock cursor behavior
        var sampleChange = new BsonDocument { { "exampleField", "exampleValue" } };
        _mockCursor
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<System.Threading.CancellationToken>()))
            .Returns(true) // Simulate a change being available
            .Returns(false); // Simulate no more changes

        _mockCursor
            .Setup(cursor => cursor.Current)
            .Returns(new List<BsonDocument> { sampleChange });

        _mockCollection
            .Setup(col => col.Watch(
                It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, BsonDocument>>(),
                It.IsAny<ChangeStreamOptions>(),
                null))
            .Returns(_mockCursor.Object);

        // Act
        using var enumerator = _mockCollection.Object.Watch(pipeline, options);

        var changesDetected = new List<BsonDocument>();
        while (enumerator.MoveNext())
        {
            changesDetected.AddRange(enumerator.Current);
        }

        // Assert
        Assert.AreEqual(1, changesDetected.Count);
        Assert.AreEqual("exampleValue", changesDetected[0]["exampleField"].AsString);

        // Verify methods were called as expected
        _mockCollection.Verify(col => col.Watch(
            It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, BsonDocument>>(),
            It.IsAny<ChangeStreamOptions>(),
            null), Times.Once);
    }
}
