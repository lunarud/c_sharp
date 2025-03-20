using Moq;
using NUnit.Framework;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;

[TestFixture]
public class ChangeStreamTests
{
    [Test]
    public void TestChangeStreamCursorWithFullDocument()
    {
        // Arrange
        // Create a sample TestEntity
        var testEntity = new TestEntity
        {
            Id = ObjectId.GenerateNewId(),
            Name = "TestName",
            Value = 42
        };

        // Create a sample ChangeStreamDocument
        var changeStreamDoc = new ChangeStreamDocument<TestEntity>(
            operationType: ChangeStreamOperationType.Insert,
            fullDocument: testEntity,
            ns: new ChangeStreamDocument<TestEntity>.NamespaceInfo("testDb", "testCollection"),
            documentKey: new BsonDocument("_id", testEntity.Id),
            updateDescription: null,
            clusterTime: new BsonTimestamp(1, 1),
            txnNumber: null,
            lsid: null
        );

        // Create the mock cursor
        var cursorMock = new Mock<IChangeStreamCursor<ChangeStreamDocument<TestEntity>>>();
        
        // Setup the mock to return our changeStreamDoc when Current is accessed
        cursorMock.Setup(x => x.Current)
            .Returns(new List<ChangeStreamDocument<TestEntity>> { changeStreamDoc });
        
        // Setup MoveNext to return true once then false
        cursorMock.SetupSequence(x => x.MoveNext(default))
            .Returns(true)
            .Returns(false);

        // Act
        var cursor = cursorMock.Object;
        bool hasNext = cursor.MoveNext(default);
        var result = cursor.Current;

        // Assert
        Assert.IsTrue(hasNext);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count());
        
        var retrievedDoc = result.First();
        Assert.AreEqual(ChangeStreamOperationType.Insert, retrievedDoc.OperationType);
        Assert.IsNotNull(retrievedDoc.FullDocument);
        Assert.AreEqual(testEntity.Id, retrievedDoc.FullDocument.Id);
        Assert.AreEqual(testEntity.Name, retrievedDoc.FullDocument.Name);
        Assert.AreEqual(testEntity.Value, retrievedDoc.FullDocument.Value);
    }
}

// Sample entity class for testing
public class TestEntity
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }
}

using Moq;
using NUnit.Framework;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;

[TestFixture]
public class ChangeStreamTests
{
    [Test]
    public void TestMockCursorWithChangeStreamDocument()
    {
        // Arrange
        // Sample entity
        var testEntity = new TestEntity
        {
            Id = ObjectId.GenerateNewId(),
            Name = "TestName",
            Value = 42
        };

        // Create required BsonDocuments for ChangeStreamDocument
        var resumeToken = new BsonDocument("resumeToken", "testToken");
        var nsDoc = new BsonDocument
        {
            { "db", "testDb" },
            { "coll", "testCollection" }
        };
        var docKey = new BsonDocument("_id", testEntity.Id);
        var fullDoc = testEntity.ToBsonDocument();

        // Create the ChangeStreamDocument
        var changeStreamDoc = new ChangeStreamDocument<TestEntity>(
            resumeToken,
            nsDoc,
            ChangeStreamOperationType.Update,
            docKey,
            fullDoc,
            null,           // updateDescription
            new BsonTimestamp(1, 1),
            null,           // extraElements
            null,           // clusterTimeDoc
            null,           // txnNumber
            null            // lsid
        );

        // Setup Mock cursor
        var cursorMock = new Mock<IChangeStreamCursor<ChangeStreamDocument<TestEntity>>>();
        
        // Configure the mock to return our changeStreamDoc
        cursorMock.Setup(m => m.Current)
            .Returns(new List<ChangeStreamDocument<TestEntity>> { changeStreamDoc });
        
        // Configure MoveNext behavior
        cursorMock.SetupSequence(m => m.MoveNext(default))
            .Returns(true)    // First call returns true (has data)
            .Returns(false);  // Second call returns false (no more data)

        // Act
        var cursor = cursorMock.Object;
        bool hasNext = cursor.MoveNext(default);
        var results = cursor.Current;

        // Assert
        Assert.IsTrue(hasNext, "Cursor should have data");
        Assert.IsNotNull(results, "Results should not be null");
        Assert.AreEqual(1, results.Count(), "Should return one document");

        var resultDoc = results.First();
        Assert.AreEqual(ChangeStreamOperationType.Update, resultDoc.OperationType, "Operation type should match");
        Assert.IsNotNull(resultDoc.FullDocument, "FullDocument should not be null");
        Assert.AreEqual(testEntity.Id, resultDoc.FullDocument.Id, "ID should match");
        Assert.AreEqual(testEntity.Name, resultDoc.FullDocument.Name, "Name should match");
        Assert.AreEqual(testEntity.Value, resultDoc.FullDocument.Value, "Value should match");
    }
}

// Sample entity class
public class TestEntity
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }
}



using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System.Collections.Generic;
public class ChangeStreamTest
{
    public void SetupMockCursor()
    {
        // Create a mock cursor
        var mockCursor = new Mock<IAsyncCursor<ChangeStreamDocument<BsonDocument>>>();
        // Create a sample ChangeStreamDocument
        var changeStreamDocument = new ChangeStreamDocument<BsonDocument>(new BsonDocument
        {
            { "_id", new BsonDocument { { "_data", "825E2FC2760000000B2B022C0100296E5A100496C525567BB74BD28BFD504F987082C046645F696400645E27BCC9F94B5117D894CBC30004" } } },
            { "operationType", "update" },
            { "ns", new BsonDocument { { "db", "sample_training" }, { "coll", "grades" } } },
            { "documentKey", new BsonDocument { { "_id", new ObjectId("5e27bcc9f94b5117d894cbc3") } } },
            { "updateDescription", new BsonDocument { { "updatedFields", new BsonDocument { { "comments.14", "You will learn a lot if you read the MongoDB blog!" } } }, { "removedFields", new BsonArray() } } }
        });
        // Set up the mock cursor to return the sample ChangeStreamDocument
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<System.Threading.CancellationToken>()))
                  .Returns(true)
                  .Returns(false);
        mockCursor.SetupGet(c => c.Current).Returns(new List<ChangeStreamDocument<BsonDocument>> { changeStreamDocument });
        // Use the mock cursor in your test
        var cursor = mockCursor.Object;
        while (cursor.MoveNext())
        {
            foreach (var doc in cursor.Current)
            {
                // Process the ChangeStreamDocument
                System.Console.WriteLine(doc);
            }
        }
    }
}

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
