using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

[TestFixture]
public class MyControllerTests
{
    private Mock<HttpContext> _httpContextMock;
    private Mock<IPrincipal> _principalMock;

    [SetUp]
    public void Setup()
    {
        _httpContextMock = new Mock<HttpContext>();
        _principalMock = new Mock<IPrincipal>();

        // Set up the user with claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Mock the user
        _principalMock.Setup(p => p.Identity).Returns(identity);
        _httpContextMock.Setup(h => h.User).Returns(claimsPrincipal);
    }

    [Test]
    public async Task MyControllerAction_ReturnsSuccess_WhenUserIsAuthenticated()
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
