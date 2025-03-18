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
            
            // Clean up before each test
            await _database.DropCollectionAsync("testCollection");
            _collection = _database.GetCollection<TestDocument>("testCollection");
        }

        [Test]
        public async Task ChangeStream_WatchesInsertOperation()
        {
            // Arrange
            var changesCaught = new List<ChangeStreamDocument<TestDocument>>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            // Start watching changes
            var pipeline = PipelineDefinition<ChangeStreamDocument<TestDocument>, ChangeStreamDocument<TestDocument>>.Create(
                new EmptyPipelineDefinition<ChangeStreamDocument<TestDocument>>());
            
            var changeStreamTask = Task.Run(async () =>
            {
                using var cursor = await _collection.WatchAsync(pipeline, cancellationToken: cts.Token);
                await cursor.ForEachAsync(change => changesCaught.Add(change));
            });

            // Act
            await Task.Delay(100); // Give change stream time to start
            var testDoc = new TestDocument { Id = "1", Name = "Test", Value = 100 };
            await _collection.InsertOneAsync(testDoc);

            // Wait for change to be detected
            await Task.Delay(500);
            cts.Cancel();

            // Assert
            Assert.That(changesCaught, Has.Count.EqualTo(1));
            Assert.That(changesCaught[0].OperationType, Is.EqualTo(ChangeStreamOperationType.Insert));
            Assert.That(changesCaught[0].FullDocument.Name
