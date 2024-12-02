using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class MongoChangeStreamService : BackgroundService
{
    private readonly ILogger<MongoChangeStreamService> _logger;
    private readonly string _connectionString;
    private readonly string _databaseName;
    private readonly string _collectionName;

    public MongoChangeStreamService(
        ILogger<MongoChangeStreamService> logger,
        string connectionString,
        string databaseName,
        string collectionName)
    {
        _logger = logger;
        _connectionString = connectionString;
        _databaseName = databaseName;
        _collectionName = collectionName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MongoChangeStreamService is starting.");
        
        var client = new MongoClient(_connectionString);
        var database = client.GetDatabase(_databaseName);
        var collection = database.GetCollection<BsonDocument>(_collectionName);

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

        var options = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
        };

        using var cursor = collection.Watch(pipeline, options);

        _logger.LogInformation("MongoChangeStreamService is now watching for changes.");
        
        try
        {
            while (!stoppingToken.IsCancellationRequested && cursor.MoveNext(stoppingToken))
            {
                foreach (var change in cursor.Current)
                {
                    _logger.LogInformation("Change detected: {Change}", change.ToJson());
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("MongoChangeStreamService is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in MongoChangeStreamService.");
        }
    }
}
