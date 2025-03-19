using NUnit.Framework;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

[TestFixture]
public class MongoChangeStreamTests
{
    private Mock<MongoCollectionWrapper<TestEntity>> _collectionMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private TestService _service;
    private TestRepository _repository;

    [SetUp]
    public void Setup()
    {
        _collectionMock = new Mock<MongoCollectionWrapper<TestEntity>>();
        _repository = new TestRepository(_collectionMock.Object);
        
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.SetupGet(u => u.TestRepository).Returns(_repository);
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new TestService(_unitOfWorkMock.Object);
    }

    [Test]
    public async Task Test_ChangeStream_Returns_FullDocument_After_Insert()
    {
        // Arrange
        var testEntity = new TestEntity 
        { 
            Id = ObjectId.GenerateNewId(),
            Name = "Inserted Entity", 
            Value = 42 
        };

        var changeDoc = new ChangeStreamDocument<TestEntity>(
            operationType: ChangeStreamOperationType.Insert,
            fullDocument: testEntity,
            ns: new BsonDocument { { "db", "test_db" }, { "coll", "test_collection" } },
            documentKey: new BsonDocument { { "_id", testEntity.Id } },
            updateDescription: null,
            clusterTime: new BsonTimestamp(1),
            txnNumber: null,
            lsid: null);

        var cursorMock = new Mock<IChangeStreamCursor<ChangeStreamDocument<TestEntity>>>();
        
        // Setup cursor to return our change document once, then complete
        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)  // First call returns true with our change
            .ReturnsAsync(false); // Second call ends the stream
        
        cursorMock.SetupGet(c => c.Current)
            .Returns(new List<ChangeStreamDocument<TestEntity>> { changeDoc });

        // Mock the Watch method to return our cursor
        _collectionMock.Setup(c => c.Watch(
            It.IsAny<IPipelineDefinition<ChangeStreamDocument<TestEntity>, ChangeStreamDocument<TestEntity>>>(),
            It.Is<ChangeStreamOptions>(o => 
                o.FullDocument == ChangeStreamFullDocumentOption.UpdateLookup && 
                o.BatchSize == 100),
            It.IsAny<CancellationToken>()))
            .Returns(cursorMock.Object);

        // Mock the insert operation
        _collectionMock.Setup(c => c.InsertOneAsync(
            It.Is<TestEntity>(e => e.Name == testEntity.Name && e.Value == testEntity.Value),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        // Start monitoring changes in background
        TestEntity receivedEntity = null;
        var monitorTask = _service.MonitorChangesAsync(change =>
        {
            receivedEntity = change.FullDocument;
        }, CancellationToken.None);

        // Perform the insert
        await _service.InsertEntityAsync(testEntity);

        // Wait for monitoring to complete
        await monitorTask;

        // Assert
        Assert.IsNotNull(receivedEntity, "Should receive a full document");
        Assert.AreEqual(testEntity.Id, receivedEntity.Id, "Entity IDs should match");
        Assert.AreEqual(testEntity.Name, receivedEntity.Name, "Entity names should match");
        Assert.AreEqual(testEntity.Value, receivedEntity.Value, "Entity values should match");

        // Verify interactions
        _collectionMock.Verify(c => c.InsertOneAsync(
            It.IsAny<TestEntity>(),
            It.IsAny<CancellationToken>()), Times.Once());

        _collectionMock.Verify(c => c.Watch(
            It.IsAny<IPipelineDefinition<ChangeStreamDocument<TestEntity>, ChangeStreamDocument<TestEntity>>>(),
            It.IsAny<ChangeStreamOptions>(),
            It.IsAny<CancellationToken>()), Times.Once());

        cursorMock.Verify(c => c.MoveNextAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [TearDown]
    public void TearDown()
    {
        _unitOfWorkMock.Object.Dispose();
    }
}

// Required supporting classes (unchanged from previous example)
public class TestEntity
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }
}

public interface ITestRepository
{
    Task<IChangeStreamCursor<ChangeStreamDocument<TestEntity>>> GetChangeStreamAsync(
        CancellationToken cancellationToken = default);
    Task InsertAsync(TestEntity entity, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork : IDisposable
{
    ITestRepository TestRepository { get; }
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}

public class MongoCollectionWrapper<T>
{
    private readonly IMongoCollection<T> _collection;
    public MongoCollectionWrapper(IMongoCollection<T> collection) => _collection = collection;
    public virtual Task InsertOneAsync(T document, CancellationToken cancellationToken = default) 
        => _collection.InsertOneAsync(document, null, cancellationToken);
    public virtual IChangeStreamCursor<ChangeStreamDocument<T>> Watch(
        IPipelineDefinition<ChangeStreamDocument<T>, ChangeStreamDocument<T>> pipeline,
        ChangeStreamOptions options,
        CancellationToken cancellationToken = default)
        => _collection.Watch(pipeline, options, cancellationToken);
}

public class TestRepository : ITestRepository
{
    private readonly MongoCollectionWrapper<TestEntity> _collectionWrapper;
    public TestRepository(MongoCollectionWrapper<TestEntity> collectionWrapper) => _collectionWrapper = collectionWrapper;
    public Task<IChangeStreamCursor<ChangeStreamDocument<TestEntity>>> GetChangeStreamAsync(
        CancellationToken cancellationToken = default)
    {
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<TestEntity>>()
            .Match(change => change.OperationType == ChangeStreamOperationType.Insert);
        var options = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
            BatchSize = 100
        };
        return Task.FromResult(_collectionWrapper.Watch(pipeline, options, cancellationToken));
    }
    public Task InsertAsync(TestEntity entity, CancellationToken cancellationToken = default)
        => _collectionWrapper.InsertOneAsync(entity, cancellationToken);
}

public class TestService
{
    private readonly IUnitOfWork _unitOfWork;
    public TestService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;
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