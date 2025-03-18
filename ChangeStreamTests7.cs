using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

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
        public async Task WatchChangesAsync_CallsWatchAsyncOnCollection()
        {
            // Arrange
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var mockCursor = new Mock<IAsyncCursor<ChangeStreamDocument<BsonDocument>>>();
            
            _ampsConfigCollectionMock
                .Setup(c => c.WatchAsync(
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            var repository = new AmpsConfigRepository(_unitOfWorkMock.Object);

            // Act
            await repository.WatchChangesAsync(cts.Token);

            // Assert
            _ampsConfigCollectionMock.Verify(
                c => c.WatchAsync(
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    cts.Token),
                Times.Once());
        }

        [Test]
        public async Task WatchChangesAsync_HandlesInsertOperation()
        {
            // Arrange
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var mockCursor = new Mock<IAsyncCursor<ChangeStreamDocument<BsonDocument>>>();
            var changeStreamDocs = new[]
            {
                new ChangeStreamDocument<BsonDocument>(
                    operationType: ChangeStreamOperationType.Insert,
                    fullDocument: new BsonDocument { { "key", "value" } },
                    ns: new BsonDocument(),
                    documentKey: new BsonDocument(),
                    updateDescription: null,
                    clusterTime: new BsonTimestamp(1),
                    txnNumber: null,
                    lsid: null)
            };

            mockCursor.Setup(c => c.MoveNextAsync(cts.Token))
                .ReturnsAsync(true)
                .Callback(() => cts.Cancel());
            mockCursor.Setup(c => c.Current).Returns(changeStreamDocs);

            _ampsConfigCollectionMock
                .Setup(c => c.WatchAsync(
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            var repository = new AmpsConfigRepository(_unitOfWorkMock.Object);

            // Act
            await repository.WatchChangesAsync(cts.Token);

            // Assert
            mockCursor.Verify(c => c.MoveNextAsync(cts.Token), Times.Once());
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
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

        public AmpsConfigRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task WatchChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_unitOfWork.AmpsConfig == null)
                throw new ArgumentNullException(nameof(_unitOfWork.AmpsConfig));

            using var cursor = await _unitOfWork.AmpsConfig.WatchAsync(cancellationToken: cancellationToken);
            await cursor.ForEachAsync(change =>
            {
                Console.WriteLine($"Operation: {change.OperationType}");
            }, cancellationToken);
        }
    }
}
