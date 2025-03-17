using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoChangeStreamTests
{
    public class ChangeStreamTest
    {
        private IMongoClient _client;
        private IMongoDatabase _database;
        private IMongoCollection<BsonDocument> _collection;
        private string _databaseName = "TestDb";
        private string _collectionName = "TestCollection";

        [SetUp]
        public void Setup()
        {
            _client = new MongoClient("mongodb://localhost:27017");
            _database = _client.GetDatabase(_databaseName);
            _collection = _database.GetCollection<BsonDocument>(_collectionName);

            // Ensure a clean collection before each test
            _database.DropCollection(_collectionName);
        }

        [Test]
        public async Task MongoDB_ChangeStream_ShouldDetectInsertion()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            var changeDetected = false;

            // Start the change stream
            var changeStreamTask = Task.Run(async () =>
            {
                using var cursor = _collection.Watch();
                await foreach (var change in cursor.ToAsyncEnumerable(cancellationTokenSource.Token))
                {
                    if (change.OperationType == ChangeStreamOperationType.Insert)
                    {
                        changeDetected = true;
                        cancellationTokenSource.Cancel(); // Stop listening after detecting the change
                    }
                }
            });

            // Wait briefly to ensure the listener is ready
            await Task.Delay(1000);

            // Insert a document
            var document = new BsonDocument { { "name", "TestDoc" }, { "value", 42 } };
            await _collection.InsertOneAsync(document);

            try
            {
                // Wait for the change stream to detect the insertion
                await changeStreamTask;
            }
            catch (OperationCanceledException)
            {
                // Expected cancellation once the change is detected
            }

            Assert.IsTrue(changeDetected, "Change stream did not detect the document insertion.");
        }

        [TearDown]
        public void Cleanup()
        {
            _database.DropCollection(_collectionName);
        }
    }
}
