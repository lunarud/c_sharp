using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ChangeStreamTests
{
    public class ChangeStreamTest
    {
        private Mock<IMongoCollection<BsonDocument>> _mockCollection;
        private Mock<IAsyncCursor<ChangeStreamDocument<BsonDocument>>> _mockCursor;

        [SetUp]
        public void SetUp()
        {
            _mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            _mockCursor = new Mock<IAsyncCursor<ChangeStreamDocument<BsonDocument>>>();
        }

        [Test]
        public void ShouldDetectInsertOperationFromChangeStream()
        {
            // Arrange
            var insertDocument = new ChangeStreamDocument<BsonDocument>(
                new BsonDocument("key", "value"),  // FullDocument
                new BsonDocument("id", new ObjectId()), // ResumeToken
                ChangeStreamOperationType.Insert,  // OperationType
                new BsonDocument("_id", new ObjectId()),  // DocumentKey
                "test.ns",  // Namespace
                new BsonDocument("updateDescription", "none"),  // UpdateDescription
                null);  // ClusterTime

            var mockResults = new List<ChangeStreamDocument<BsonDocument>> { insertDocument };

            // Configure the mock cursor
            _mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<System.Threading.CancellationToken>()))
                       .Returns(true)  // First call returns data
                       .Returns(false); // Subsequent call ends iteration

            _mockCursor.Setup(c => c.Current).Returns(mockResults);

            // Configure the collection's Watch method
            _mockCollection.Setup(c => c.Watch(
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    It.IsAny<System.Threading.CancellationToken>()
                )).Returns(_mockCursor.Object);

            // Act
            var changeStream = _mockCollection.Object.Watch();
            var detectedOperations = new List<ChangeStreamOperationType>();

            while (changeStream.MoveNext())
            {
                detectedOperations.AddRange(changeStream.Current.Select(doc => doc.OperationType));
            }

            // Assert
            Assert.That(detectedOperations.Count, Is.EqualTo(1));
            Assert.That(detectedOperations.First(), Is.EqualTo(ChangeStreamOperationType.Insert));
        }
    }
}
