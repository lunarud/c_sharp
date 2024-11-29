using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;

[TestFixture]
public class ChangeStreamExecuteTests
{
    private Mock<ILogger<ClassContainingChangeStreamExecute>> _loggerMock;
    private Mock<IMongoClient> _mongoClientMock;
    private Mock<IMongoDatabase> _mongoDatabaseMock;
    private Mock<IMongoCollection<ChangeRecord>> _collectionMock;
    private Mock<IChangeStreamCursor<ChangeStreamDocument<ChangeRecord>>> _cursorMock;
    private Mock<IApiUnitOfWork> _apiUnitOfWorkMock;

    private ClassContainingChangeStreamExecute _service;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ClassContainingChangeStreamExecute>>();
        _mongoClientMock = new Mock<IMongoClient>();
        _mongoDatabaseMock = new Mock<IMongoDatabase>();
        _collectionMock = new Mock<IMongoCollection<ChangeRecord>>();
        _cursorMock = new Mock<IChangeStreamCursor<ChangeStreamDocument<ChangeRecord>>>();
        _apiUnitOfWorkMock = new Mock<IApiUnitOfWork>();

        // Setup MongoDB mocks
        _mongoClientMock
            .Setup(client => client.GetDatabase(It.IsAny<string>(), null))
            .Returns(_mongoDatabaseMock.Object);

        _mongoDatabaseMock
            .Setup(db => db.GetCollection<ChangeRecord>(It.IsAny<string>(), null))
            .Returns(_collectionMock.Object);

        // Instantiate the service
        _service = new ClassContainingChangeStreamExecute(
            _loggerMock.Object,
            _mongoClientMock.Object,
            _apiUnitOfWorkMock.Object);
    }

    [Test]
    public async Task ChangeStreamExecute_ShouldProcessChangeStreamRecords()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Mock ChangeStreamCursor behavior
        var mockChangeRecord = new ChangeStreamDocument<ChangeRecord>
        {
            FullDocument = new ChangeRecord
            {
                Id = Guid.NewGuid().ToString()
            }
        };

        _cursorMock
            .SetupSequence(cursor => cursor.MoveNextAsync(cancellationToken))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _cursorMock
            .Setup(cursor => cursor.Current)
            .Returns(new List<ChangeStreamDocument<ChangeRecord>> { mockChangeRecord });

        _collectionMock
            .Setup(collection => collection.WatchAsync(
                It.IsAny<PipelineDefinition<ChangeStreamDocument<ChangeRecord>, ChangeStreamDocument<ChangeRecord>>>(),
                It.IsAny<ChangeStreamOptions>(),
                cancellationToken))
            .ReturnsAsync(_cursorMock.Object);

        _apiUnitOfWorkMock
            .Setup(repo => repo.ChangeRecordsFlatRepo.CreateAsync(It.IsAny<ChangeRecordFlat>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ChangeStreamExecute(cancellationToken);

        // Assert
        _collectionMock.Verify(collection => collection.WatchAsync(
            It.IsAny<PipelineDefinition<ChangeStreamDocument<ChangeRecord>, ChangeStreamDocument<ChangeRecord>>>(),
            It.IsAny<ChangeStreamOptions>(),
            cancellationToken), Times.Once);

        _apiUnitOfWorkMock.Verify(repo => repo.ChangeRecordsFlatRepo.CreateAsync(It.IsAny<ChangeRecordFlat>()), Times.AtLeastOnce);

        _loggerMock.Verify(logger => logger.LogInformation(It.IsAny<string>()), Times.AtLeastOnce);
        _loggerMock.Verify(logger => logger.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
    }
}