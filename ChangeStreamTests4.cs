using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NUnit.Framework;
using System.Threading;
using System.Collections.Generic;

namespace MongoDBTests
{
    [TestFixture]
    public class ChangeStreamTests
    {
        private IMongoDatabase _database;
        private IMongoCollection<TestDocument> _collection;
        private readonly string _connectionString = "mongodb://localhost:27017";
        private readonly string _databaseName = "TestDB";

        public class TestDocument
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int Value { get; set; }
        }

        [SetUp]
        public async Task Setup()
        {
            var client = new MongoClient(_connectionString);
            _database = client.GetDatabase(_databaseName);
            await _database.DropCollectionAsync("testCollection");
            _collection = _database.GetCollection<TestDocument>("testCollection");
        }

        [Test]
        public async Task ChangeStream_WatchesInsertOperation()
        {
            // Arrange
            var changesCaught = new List<ChangeStreamDocument<TestDocument>>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            // Start watching changes (no pipeline)
            var changeStreamTask = Task.Run(async () =>
            {
                using var cursor = await _collection.WatchAsync(cancellationToken: cts.Token);
                await cursor.ForEachAsync(change => changesCaught.Add(change));
            });

            // Act
            await Task.Delay(100);
            var testDoc = new TestDocument { Id = "1", Name = "Test", Value = 100 };
            await _collection.InsertOneAsync(testDoc);

            await Task.Delay(500);
            cts.Cancel();

            // Assert
            Assert.That(changesCaught, Has.Count.EqualTo(1));
            Assert.That(changesCaught[0].OperationType, Is.EqualTo(ChangeStreamOperationType.Insert));
            Assert.That(changesCaught[0].FullDocument.Name, Is.EqualTo("Test"));
        }

        [Test]
        public async Task ChangeStream_WatchesUpdateOperation()
        {
            // Arrange
            var initialDoc = new TestDocument { Id = "2", Name = "Initial", Value = 100 };
            await _collection.InsertOneAsync(initialDoc);
            
            var changesCaught = new List<ChangeStreamDocument<TestDocument>>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Start watching changes (no pipeline)
            var changeStreamTask = Task.Run(async () =>
            {
                using var cursor = await _collection.WatchAsync(cancellationToken: cts.Token);
                await cursor.ForEachAsync(change => changesCaught.Add(change));
            });

            // Act
            await Task.Delay(100);
            var filter = Builders<TestDocument>.Filter.Eq(d => d.Id, "2");
            var update = Builders<TestDocument>.Update.Set(d => d.Name, "Updated");
            await _collection.UpdateOneAsync(filter, update);

            await Task.Delay(500);
            cts.Cancel();

            // Assert
            Assert.That(changesCaught, Has.Count.EqualTo(1));
            Assert.That(changesCaught[0].OperationType, Is.EqualTo(ChangeStreamOperationType.Update));
            Assert.That(changesCaught[0].UpdateDescription.UpdatedFields.ContainsKey("Name"), Is.True);
        }

        [Test]
        public async Task ChangeStream_WithPipeline_WatchesSpecificOperations()
        {
            // Arrange
            var changesCaught = new List<ChangeStreamDocument<TestDocument>>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Define pipeline to only watch update operations
            var pipeline = PipelineDefinition<ChangeStreamDocument<TestDocument>, ChangeStreamDocument<TestDocument>>.Create(
                new[] { new BsonDocument("$match", new BsonDocument("operationType", "update")) }
            );

            // Start watching changes (with pipeline)
            var changeStreamTask = Task.Run(async () =>
            {
                using var cursor = await _collection.WatchAsync(pipeline, cancellationToken: cts.Token);
                await cursor.ForEachAsync(change => changesCaught.Add(change));
            });

            // Act
            await Task.Delay(100);
            await _collection.InsertOneAsync(new TestDocument { Id = "3", Name = "Insert", Value = 1 });
            await _collection.UpdateOneAsync(
                Builders<TestDocument>.Filter.Eq(d => d.Id, "3"),
                Builders<TestDocument>.Update.Set(d => d.Value, 2));

            await Task.Delay(500);
            cts.Cancel();

            // Assert - Should only catch the update, not the insert
            Assert.That(changesCaught, Has.Count.EqualTo(1));
            Assert.That(changesCaught[0].OperationType, Is.EqualTo(ChangeStreamOperationType.Update));
        }

        [TearDown]
        public async Task TearDown()
        {
            await _database.DropCollectionAsync("testCollection");
        }
    }
}
