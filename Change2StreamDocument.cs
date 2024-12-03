using Moq;
using MongoDB.Bson;
using MongoDB.Driver;

// Create a mock ChangeStreamDocument
var mockChangeStreamDocument = new Mock<ChangeStreamDocument<BsonDocument>>();

mockChangeStreamDocument.Setup(doc => doc.OperationType).Returns(ChangeStreamOperationType.Insert);
mockChangeStreamDocument.Setup(doc => doc.FullDocument).Returns(new BsonDocument { { "field", "value" } });
mockChangeStreamDocument.Setup(doc => doc.DocumentKey).Returns(new BsonDocument { { "_id", 1 } });

// Use the mocked object in your tests
var testChangeDocument = mockChangeStreamDocument.Object;
