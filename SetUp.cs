[SetUp]
public async Task SetUp()
{
    await _collection.DeleteManyAsync(FilterDefinition<Product>.Empty);

    var products = new[]
    {
        new Product { Category = "Books", Price = 10 },
        new Product { Category = "Books", Price = 15 },
        new Product { Category = "Electronics", Price = 100 },
        new Product { Category = "Electronics", Price = 150 },
    };

    await _collection.InsertManyAsync(products);
}

[Test]
public async Task Test_Aggregate_GroupByCategory_SumsPrices_StronglyTyped()
{
    // Act
    var aggregation = _collection.Aggregate()
        .Group(
            p => p.Category,
            g => new CategoryTotal
            {
                Category = g.Key,
                TotalPrice = g.Sum(p => p.Price)
            });

    var resultList = await aggregation.ToListAsync();

    // Assert
    Assert.AreEqual(2, resultList.Count);

    foreach (var categoryTotal in resultList)
    {
        if (categoryTotal.Category == "Books")
        {
            Assert.AreEqual(25, categoryTotal.TotalPrice);
        }
        else if (categoryTotal.Category == "Electronics")
        {
            Assert.AreEqual(250, categoryTotal.TotalPrice);
        }
        else
        {
            Assert.Fail("Unexpected category found.");
        }
    }
}
