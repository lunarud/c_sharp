using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

// Domain Entity
public class TestEntity
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }
}

// Repository Interface
public interface ITestRepository
{
    Task<IChangeStreamCursor<ChangeStreamDocument<TestEntity>>> GetChangeStreamAsync(
        CancellationToken cancellationToken = default);
    Task InsertAsync(TestEntity entity, CancellationToken cancellationToken = default);
}

// Unit of Work Interface
public interface IUnitOfWork : IDisposable
{
    ITestRepository TestRepository { get; }
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}

// MongoDB Collection Wrapper
public class MongoCollectionWrapper<T>
{
    private readonly IMongoCollection<T> _collection;
    public MongoCollectionWrapper(IMongoCollection<T> collection)
    {
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public virtual Task InsertOneAsync(T document, CancellationToken cancellationToken = default) 
        => _collection.InsertOneAsync(document, null, cancellationToken);

    public virtual IChangeStreamCursor<ChangeStreamDocument<T>> Watch(
        IPipelineDefinition<ChangeStreamDocument<T>, ChangeStreamDocument<T>> pipeline,
        ChangeStreamOptions options,
        CancellationToken cancellationToken = default)
        => _collection.Watch(pipeline, options, cancellationToken);
}

// Repository Implementation
public class TestRepository : ITestRepository
{
    private readonly MongoCollectionWrapper<TestEntity> _collectionWrapper;

    public TestRepository(MongoCollectionWrapper<TestEntity> collectionWrapper)
    {
        _collectionWrapper = collectionWrapper ?? throw new ArgumentNullException(nameof(collectionWrapper));
    }

    public async Task<IChangeStreamCursor<ChangeStreamDocument<TestEntity>>> GetChangeStreamAsync(
        CancellationToken cancellationToken = default)
    {
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<TestEntity>>()
            .Match(change => change.OperationType == ChangeStreamOperationType.Insert);

        var options = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
            BatchSize = 100
        };

        return await Task.FromResult(_collectionWrapper.Watch(pipeline, options, cancellationToken));
    }

    public Task InsertAsync(TestEntity entity, CancellationToken cancellationToken = default)
        => _collectionWrapper.InsertOneAsync(entity, cancellationToken);
}

// Unit of Work Implementation
public class UnitOfWork : IUnitOfWork
{
    private readonly IMongoDatabase _database;
    private bool _disposed;

    public UnitOfWork(IMongoClient mongoClient)
    {
        _database = mongoClient.GetDatabase("test_db");
        TestRepository = new TestRepository(
            new MongoCollectionWrapper<TestEntity>(_database.GetCollection<TestEntity>("test_collection")));
    }

    public ITestRepository TestRepository { get; }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default) 
        => Task.FromResult(1); // In MongoDB, operations are atomic by default

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

// Service Layer
public class TestService
{
    private readonly IUnitOfWork _unitOfWork;

    public TestService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task MonitorChangesAsync(Action<ChangeStreamDocument<TestEntity>> onChange, 
        CancellationToken cancellationToken = default)
    {
        using var cursor = await _unitOfWork.TestRepository.GetChangeStreamAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken) && !cancellationToken.IsCancellationRequested)
        {
            foreach (var change in cursor.Current)
            {
                onChange(change);
            }
        }
    }

    public async Task InsertEntityAsync(TestEntity entity, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.TestRepository.InsertAsync(entity, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}

// Unit Tests with Moq
[TestFixture]
public class MongoChangeStreamTests
{
    private Mock<MongoCollectionWrapper<TestEntity>> _collectionMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private TestService _service;

    [SetUp]
    public void Setup()
    {
        _collectionMock = new Mock<MongoCollectionWrapper<TestEntity>>();
        var repository = new TestRepository(_collectionMock.Object);
        
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.SetupGet(u => u.TestRepository).Returns(repository);
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new TestService(_unitOfWorkMock.Object);
    }

    [Test]
    public async Task Test_ChangeStream_Service_Processes_Insert()
    {
        // Arrange
        var cursorMock = new Mock<IChangeStreamCursor<ChangeStreamDocument<TestEntity>>>();
        var changeDoc = new ChangeStreamDocument<TestEntity>(
            operationType: ChangeStreamOperationType.Insert,
            fullDocument: new TestEntity { Name = "Test", Value = 42 },
            ns: new BsonDocument(),
            documentKey: new BsonDocument(),
            updateDescription: null,
            clusterTime: new BsonTimestamp(1),
            txnNumber: null,
            lsid: null);

        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        cursorMock.SetupGet(c => c.Current).Returns(new[] { changeDoc });

        _collectionMock.Setup(c => c.Watch(
            It.IsAny<IPipelineDefinition<ChangeStreamDocument<TestEntity>, ChangeStreamDocument<TestEntity>>>(),
            It.IsAny<ChangeStreamOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(cursorMock.Object);

        // Act
        TestEntity receivedEntity = null;
        await _service.MonitorChangesAsync(change =>
        {
            receivedEntity = change.FullDocument;
        }, CancellationToken.None);

        // Assert
        Assert.IsNotNull(receivedEntity);
        Assert.AreEqual("Test", receivedEntity.Name);
        Assert.AreEqual(42, receivedEntity.Value);
        
        _collectionMock.Verify(c => c.Watch(
            It.IsAny<IPipelineDefinition<ChangeStreamDocument<TestEntity>, ChangeStreamDocument<TestEntity>>>(),
            It.Is<ChangeStreamOptions>(o => o.BatchSize == 100),
            It.IsAny<CancellationToken>()), Times.Once());
    }
}