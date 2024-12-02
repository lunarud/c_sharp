var collection = new MongoClient("<connection-string>")
    .GetDatabase("<database-name>")
    .GetCollection<BsonDocument>("<collection-name>");

var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
    .Match(change => 
        change.OperationType == ChangeStreamOperationType.Insert || 
        change.OperationType == ChangeStreamOperationType.Update || 
        change.OperationType == ChangeStreamOperationType.Replace
    )
    .AppendStage<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>, BsonDocument>(
        @"{ 
            $project: { 
                '_id': 1, 
                'fullDocument': 1, 
                'ns': 1, 
                'documentKey': 1 
            }
        }"
    );

ChangeStreamOptions options = new ()
{
    FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
};

using IChangeStreamCursor<BsonDocument> enumerator = collection.Watch(
    pipeline, 
    options
);

Console.WriteLine("Waiting for changes...");
while (enumerator.MoveNext())
{
    IEnumerable<BsonDocument> changes = enumerator.Current;
    foreach(BsonDocument change in changes)
    {
        Console.WriteLine(change);
    }  
}
