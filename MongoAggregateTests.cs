[TestFixture]
public class MongoAggregateTests
{
    private IMongoCollection<Product> _productCollection;

    [SetUp]
    public void Setup()
    {
        // ... (Connect to your MongoDB database and get the 'products' collection)
    }

    [Test]
    public void TestCountDocuments()
    {
        var filter = Builders<Product>.Filter.Empty;
        var count = _productCollection.CountDocuments(filter);

        Assert.That(count, Is.GreaterThan(0));
    }

    [Test]
    public void TestAveragePrice()
    {
        var averagePrice = _productCollection.Aggregate()
            .Group(x => 1, g => new { AveragePrice = g.Average(p => p.Price) })
            .FirstOrDefaultAsync()
            .Result.AveragePrice;

        Assert.That(averagePrice, Is.GreaterThan(0));
    }
}
