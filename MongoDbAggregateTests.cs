using NUnit.Framework;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;

namespace YourNamespace.Tests
{
    [TestFixture]
    public class MongoDbAggregateTests
    {
        private IMongoClient _client;
        private IMongoDatabase _database;
        private IMongoCollection<BsonDocument> _collection;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Connect to the MongoDB test database
            var connectionString = "mongodb://localhost:27017"; // Replace with your test DB connection string
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase("TestDatabase");
            _collection = _database.GetCollection<BsonDocument>("TestCollection");
        }

        [SetUp]
        public async Task SetUp()
        {
            // Clean up the collection before each test
            await _collection.DeleteManyAsync(new BsonDocument());

            // Insert test data
            var documents = new[]
            {
                new BsonDocument { { "Category", "Books" }, { "Price", 10 } },
                new BsonDocument { { "Category", "Books" }, { "Price", 15 } },
                new BsonDocument { { "Category", "Electronics" }, { "Price", 100 } },
                new BsonDocument { { "Category", "Electronics" }, { "Price", 150 } },
            };

            await _collection.InsertManyAsync(documents);
        }

        [TearDown]
        public async Task TearDown()
        {
            // Optional: Clean up after each test
            await _collection.DeleteManyAsync(new BsonDocument());
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Optional: Drop the test database after all tests are done
            _client.DropDatabase("TestDatabase");
        }

        // ... Test methods go here ...
    }
}
