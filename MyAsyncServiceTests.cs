using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class MyAsyncServiceTests
{
    private MyAsyncService _service;

    [SetUp]
    public void Setup()
    {
        _service = new MyAsyncService();
    }

    [Test]
    public async Task MyAsyncMethod_CompletesSuccessfully_WhenNotCancelled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        // Act
        var result = await _service.MyAsyncMethod(token);

        // Assert
        Assert.IsTrue(result);
    }

    [Test]
    public void MyAsyncMethod_ThrowsOperationCanceledException_WhenCancelled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        // Cancel the token
        cancellationTokenSource.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () => await _service.MyAsyncMethod(token));
    }
}
