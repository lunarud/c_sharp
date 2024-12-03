[Test]
public async Task Test_Aggregate_GroupByCategory_SumsPrices()
{
    // Arrange: (SetUp method already inserts test data)

    // Act: Perform the aggregation
    var pipeline = new[]
    {
        new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Category" },
                { "TotalPrice", new BsonDocument("$sum", "$Price") }
            })
    };

    var result = await _collection.AggregateAsync<BsonDocument>(pipeline);
    var resultList = await result.ToListAsync();

    // Assert: Verify the aggregation results
    Assert.AreEqual(2, resultList.Count);

    foreach (var doc in resultList)
    {
        var category = doc["_id"].AsString;
        var totalPrice = doc["TotalPrice"].AsInt32;

        if (category == "Books")
        {
            Assert.AreEqual(25, totalPrice);
        }
        else if (category == "Electronics")
        {
            Assert.AreEqual(250, totalPrice);
        }
        else
        {
            Assert.Fail("Unexpected category found.");
        }
    }
}
