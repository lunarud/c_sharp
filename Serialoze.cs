using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class PersonSerializer : IBsonSerializer<Person>
{
    public Type ValueType => typeof(Person);

    public Person Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonReader = context.Reader;

        bsonReader.ReadStartDocument();
        var name = bsonReader.ReadString("Name");
        var age = bsonReader.ReadInt32("Age");
        bsonReader.ReadEndDocument();

        return new Person { Name = name, Age = age };
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Person value)
    {
        var bsonWriter = context.Writer;

        bsonWriter.WriteStartDocument();
        bsonWriter.WriteString("Name", value.Name);
        bsonWriter.WriteInt32("Age", value.Age);
        bsonWriter.WriteEndDocument();
    }
}

class Program
{
    static void Main()
    {
        // Register the custom serializer
        BsonSerializer.RegisterSerializer(new PersonSerializer());

        // Create a sample Person object
        var person = new Person { Name = "Alice", Age = 25 };

        // Serialize the Person object
        var serializedData = person.ToBson();

        Console.WriteLine("Serialized Data:");
        Console.WriteLine(BitConverter.ToString(serializedData));

        // Deserialize the byte array back into a Person object
        var deserializedPerson = BsonSerializer.Deserialize<Person>(serializedData);

        Console.WriteLine("\nDeserialized Object:");
        Console.WriteLine($"Name: {deserializedPerson.Name}, Age: {deserializedPerson.Age}");
    }
}