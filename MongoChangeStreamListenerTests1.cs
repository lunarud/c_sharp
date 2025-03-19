using NUnit.Framework;
using MongoDB.Driver;
using MongoDB.Bson;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

[TestFixture]
public class MongoChangeStreamListenerTests
{
    [Test]
    public async Task ListenForChangesAsync_ShouldTriggerCallbackOnChange()
    {
        // Arrange
        var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
        var mockCursor = new Mock<IAsyncCursor<ChangeStreamDocument<BsonDocument>>>();

        var changeStreamDoc = new ChangeStreamDocument<BsonDocument>(
            new BsonDocument { { "_id", BsonObjectId.GenerateNewId() } });

        var changeList = new List<ChangeStreamDocument<BsonDocument>> { changeStreamDoc };
        var enumerator = changeList.GetEnumerator();

        mockCursor
            .SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)  // First call returns true (new event)
            .Returns(false); // Second call returns false (stop)

        mockCursor
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        mockCursor
            .Setup(c => c.Current)
            .Returns(() => new List<ChangeStreamDocument<BsonDocument>> { changeStreamDoc });

        mockCollection
            .Setup(c => c.WatchAsync(
                It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                It.IsAny<ChangeStreamOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        var listener = new MongoChangeStreamListener<BsonDocument>(mockCollection.Object);

        bool callbackInvoked = false;
        async Task TestCallback(ChangeStreamDocument<BsonDocument> change)
        {
            callbackInvoked = true;
            await Task.CompletedTask;
        }

        // Act
        await listener.ListenForChangesAsync(TestCallback, CancellationToken.None);

        // Assert
        Assert.IsTrue(callbackInvoked, "Callback should have been invoked when a change is detected.");
    }
}
