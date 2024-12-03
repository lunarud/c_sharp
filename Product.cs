public class Product
{
    public ObjectId Id { get; set; }
    public string Category { get; set; }
    public int Price { get; set; }
}

public class CategoryTotal
{
    public string Category { get; set; }
    public int TotalPrice { get; set; }
}
