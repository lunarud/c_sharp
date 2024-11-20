db.records.aggregate([
  // Sort the records globally by `id` and `timestamp`
  { 
    $sort: { id: 1, timestamp: -1 } 
  },
  // Group by `id` and collect the top 2 records for each group
  { 
    $group: { 
      _id: "$id",
      topRecords: { $push: "$$ROOT" } // Collect all records for each `id`
    }
  },
  // Slice the top 2 records for each group
  { 
    $project: { 
      _id: 1, 
      topRecords: { $slice: ["$topRecords", 2] } 
    }
  }
]);

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;

public class Program
{
    public static void Main()
    {
        // Example BSON document
        var bsonDocument = new BsonDocument
        {
            { "name", "John Doe" },
            { "timestamp", new BsonDateTime(DateTime.UtcNow) }
        };

        // Convert BSON document to C# object
        var myObject = BsonSerializer.Deserialize<MyClass>(bsonDocument);

        // Serialize object to JSON, converting ISODate to string
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new IsoDateJsonConverter() }
        };

        string json = JsonSerializer.Serialize(myObject, jsonOptions);
        Console.WriteLine(json);
    }
}

// C# class representing the BSON document
public class MyClass
{
    public string Name { get; set; }
    public DateTime Timestamp { get; set; }
}

// Custom JsonConverter to handle ISODate
public class IsoDateJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!); // Convert string back to DateTime
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")); // Serialize to ISO 8601 string
    }
}