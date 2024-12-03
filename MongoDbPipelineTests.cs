using NUnit.Framework;
using Moq;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class MongoDbPipelineTests
{
    private Mock<IMongoCollection<Product>> _mockCollection;
    private Mock<IAsyncCursor<CategoryTotal>> _mockCursor;

    [SetUp]
    public void SetUp()
    {
        _mockCollection = new Mock<IMongoCollection<Product>>();
        _mockCursor = new Mock<IAsyncCursor<CategoryTotal>>();
    }

    [Test]
    public async Task Test_AggregateAsync_GroupByCategory()
    {
        // Arrange
        var sampleData = new List<CategoryTotal>
        {
            new CategoryTotal { Category = "Books", TotalPrice = 25 },
            new CategoryTotal { Category = "Electronics", TotalPrice = 250 }
        };

        // Mock the cursor's behavior
        _mockCursor
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true) // First call: has data
            .Returns(false); // Second call: no data

        _mockCursor
            .SetupGet(cursor => cursor.Current)
            .Returns(sampleData);

        // Mock AggregateAsync behavior
        _mockCollection
            .Setup(collection => collection.AggregateAsync(
                It.IsAny<PipelineDefinition<Product, CategoryTotal>>(),
                It.IsAny<AggregateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockCursor.Object);

        // Act
        var pipeline = new BsonDocument[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Category" },
                { "TotalPrice", new BsonDocument("$sum", "$Price") }
            })
        };

        var result = await _mockCollection.Object.AggregateAsync(
            pipeline: pipeline,
            options: null,
            cancellationToken: CancellationToken.None);

        var resultList = result.ToList();

        // Assert
        Assert.AreEqual(2, resultList.Count);
        Assert.AreEqual("Books", resultList[0].Category);
        Assert.AreEqual(25, resultList[0].TotalPrice);
        Assert.AreEqual("Electronics", resultList[1].Category);
        Assert.AreEqual(250, resultList[1].TotalPrice);
    }
}
