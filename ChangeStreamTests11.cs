using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;

namespace MongoDBTests
{
    public interface IUnitOfWork : IDisposable
    {
        IMongoCollection<BsonDocument> AmpsConfig { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    public interface ISingleValueRepository<T>
    {
        Task WatchChangesAsync(CancellationToken cancellationToken = default);
        event EventHandler<ChangeStreamDocument<BsonDocument>> ChangeDetected;
    }

    [TestFixture]
    public class AmpsConfigChangeStreamTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IMongoCollection<BsonDocument>> _ampsConfigCollectionMock;
        private Mock<ISingleValueRepository<BsonDocument>> _repositoryMock;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _ampsConfigCollectionMock = new Mock<IMongoCollection<BsonDocument>>();
            _repositoryMock = new Mock<ISingleValueRepository<BsonDocument>>();

            _unitOfWorkMock.Setup(u => u.AmpsConfig).Returns(_ampsConfigCollectionMock.Object);
        }

        [Test]
        public async Task WatchChangesAsync_InvokesRepositoryMethod()
        {
            // Arrange
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            _repositoryMock
                .Setup(r => r.WatchChangesAsync(cts.Token))
                .Returns(Task.CompletedTask);

            // Act
            await _repositoryMock.Object.WatchChangesAsync(cts.Token);

            // Assert
            _repositoryMock.Verify(
                r => r.WatchChangesAsync(cts.Token),
                Times.Once());
        }

        [Test]
        public async Task WatchChangesAsync_RaisesChangeDetectedEvent_OnInsert()
        {
            // Arrange
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            ChangeStreamDocument<BsonDocument> detectedChange = null;

            var changeDoc = new ChangeStreamDocument<BsonDocument>(
                operationType: ChangeStreamOperationType.Insert,
                fullDocument: new BsonDocument { { "key", "value" } },
                ns: new BsonDocument(),
                documentKey: new BsonDocument(),
                updateDescription: null,
                clusterTime: new BsonTimestamp(1),
                txnNumber: null,
                lsid: null);

            _repositoryMock
                .Setup(r => r.WatchChangesAsync(cts.Token))
                .Callback(() => _repositoryMock.Raise(r => r.ChangeDetected += null, _repositoryMock.Object, changeDoc))
                .Returns(Task.CompletedTask);

            _repositoryMock.Object.ChangeDetected += (sender, change) => detectedChange = change;

            // Act
            await _repositoryMock.Object.WatchChangesAsync(cts.Token);

            // Assert
            Assert.That(detectedChange, Is.Not.Null);
            Assert.That(detectedChange.OperationType, Is.EqualTo(ChangeStreamOperationType.Insert));
            Assert.That(detectedChange.FullDocument["key"], Is.EqualTo(BsonValue.Create("value")));
        }

        [Test]
        public void WatchChangesAsync_ThrowsException_WhenCollectionIsNull()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.AmpsConfig).Returns((IMongoCollection<BsonDocument>)null);
            var repository = new AmpsConfigRepository(_unitOfWorkMock.Object);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(
                async () => await repository.WatchChangesAsync());
        }
    }

    public class AmpsConfigRepository : ISingleValueRepository<BsonDocument>
    {
        private readonly IUnitOfWork _unitOfWork;

        public event EventHandler<ChangeStreamDocument<BsonDocument>> ChangeDetected;

        public AmpsConfigRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task WatchChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_unitOfWork.AmpsConfig == null)
                throw new ArgumentNullException(nameof(_unitOfWork.AmpsConfig));

            // This is the real implementation we're not testing directly
            using var cursor = await _unitOfWork.AmpsConfig.WatchAsync(cancellationToken: cancellationToken);
            await cursor.ForEachAsync(change =>
            {
                ChangeDetected?.Invoke(this, change);
            }, cancellationToken);
        }
    }
}
