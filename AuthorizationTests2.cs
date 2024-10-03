using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

[TestFixture]
public class AuthorizationTests
{
    private Mock<HttpContext> _httpContextMock;

    [SetUp]
    public void Setup()
    {
        _httpContextMock = new Mock<HttpContext>();

        // Simulate the authorization with a valid token
        var token = "Bearer validToken";
        _httpContextMock.Setup(h => h.Request.Headers["Authorization"]).Returns(token);
    }

    [Test]
    public async Task MyControllerAction_ReturnsSuccess_WhenTokenIsValid()
    {
        // Arrange
        var controller = new MyController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContextMock.Object
        };

        // Act
        var result = await controller.MyAction();

        // Assert
        Assert.IsInstanceOf<OkResult>(result);
    }
}
