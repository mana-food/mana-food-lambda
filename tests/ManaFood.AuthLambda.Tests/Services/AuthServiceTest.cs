using System.IdentityModel.Tokens.Jwt;
using Amazon.DynamoDBv2.DocumentModel;
using FluentAssertions;
using ManaFood.AuthLambda.Services;
using Moq;
using Xunit;

namespace ManaFood.AuthLambda.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "MyTestSecretKeyThatIsLongEnough1234567890");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");

        _mockUserService = new Mock<IUserService>();
        _authService = new AuthService(_mockUserService.Object);
    }

    [Fact]
    public void Constructor_WithoutJwtSecretKey_ThrowsException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("Jwt__SecretKey", null);

        // Act
        var act = () => new AuthService(_mockUserService.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Jwt__SecretKey not configured");

        // Restore
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "MyTestSecretKeyThatIsLongEnough1234567890");
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsTokenAndUserData()
    {
        // Arrange
        var cpf = "12345678900";
        var password = "senha123";
        var userDoc = CreateUserDocument(cpf, password, 1);

        _mockUserService
            .Setup(x => x.GetUserByCpfAsync(cpf))
            .ReturnsAsync(userDoc);

        // Act
        var result = await _authService.AuthenticateAsync(cpf, password);

        // Assert
        result.Should().NotBeNull();
        var resultDict = GetDynamicProperties(result!);
        
        resultDict.Should().ContainKey("token");
        resultDict.Should().ContainKey("expiresIn");
        resultDict.Should().ContainKey("user");
        
        resultDict["expiresIn"].Should().Be(3600);
        
        var token = resultDict["token"] as string;
        token.Should().NotBeNullOrEmpty();
        ValidateJwtToken(token!, "CUSTOMER");
        
        var user = GetDynamicProperties(resultDict["user"]!);
        user["id"].Should().Be("user-123");
        user["name"].Should().Be("João Silva");
        user["email"].Should().Be("joao@test.com");
        user["userType"].Should().Be(1);
    }

    [Fact]
    public async Task AuthenticateAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var cpf = "99999999999";
        var password = "senha123";

        _mockUserService
            .Setup(x => x.GetUserByCpfAsync(cpf))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _authService.AuthenticateAsync(cpf, password);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WithWrongPassword_ReturnsNull()
    {
        // Arrange
        var cpf = "12345678900";
        var correctPassword = "senhaCorreta";
        var wrongPassword = "senhaErrada";
        var userDoc = CreateUserDocument(cpf, correctPassword, 1);

        _mockUserService
            .Setup(x => x.GetUserByCpfAsync(cpf))
            .ReturnsAsync(userDoc);

        // Act
        var result = await _authService.AuthenticateAsync(cpf, wrongPassword);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(0, "ADMIN")]
    [InlineData(1, "CUSTOMER")]
    [InlineData(2, "KITCHEN")]
    [InlineData(3, "OPERATOR")]
    [InlineData(4, "MANAGER")]
    [InlineData(99, "CUSTOMER")]
    public async Task AuthenticateAsync_WithDifferentUserTypes_ReturnsCorrectRole(int userType, string expectedRole)
    {
        // Arrange
        var cpf = "12345678900";
        var password = "senha123";
        var userDoc = CreateUserDocument(cpf, password, userType);

        _mockUserService
            .Setup(x => x.GetUserByCpfAsync(cpf))
            .ReturnsAsync(userDoc);

        // Act
        var result = await _authService.AuthenticateAsync(cpf, password);

        // Assert
        result.Should().NotBeNull();
        var token = GetDynamicProperties(result!)["token"] as string;
        ValidateJwtToken(token!, expectedRole);
    }

    [Fact]
    public void HashPassword_WithValidPassword_ReturnsHash()
    {
        // Arrange
        var password = "senha123";

        // Act
        var hash = AuthService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().Be("D/wWFW3VVpKgz8a89f/IUNhlhzLCObzmIZO/rpV4dAw=");
    }

    [Fact]
    public void HashPassword_WithSamePassword_ReturnsSameHash()
    {
        // Arrange
        var password = "senha123";

        // Act
        var hash1 = AuthService.HashPassword(password);
        var hash2 = AuthService.HashPassword(password);

        // Assert
        hash1.Should().Be(hash2);
    }

    private static Document CreateUserDocument(string cpf, string password, int userType)
    {
        var doc = new Document();
        doc["Id"] = "user-123";
        doc["Name"] = "João Silva";
        doc["Email"] = "joao@test.com";
        doc["Cpf"] = cpf;
        doc["Password"] = AuthService.HashPassword(password);
        doc["UserType"] = userType;
        return doc;
    }

    private static void ValidateJwtToken(string token, string expectedRole)
    {
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
        
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == "sub");
        jwtToken.Claims.Should().Contain(c => c.Type == "name");
        jwtToken.Claims.Should().Contain(c => c.Type == "email");
        jwtToken.Claims.Should().Contain(c => c.Type == "cpf");
        jwtToken.Claims.Should().Contain(c => c.Type == "jti");
        
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(expectedRole);
    }

    private static Dictionary<string, object?> GetDynamicProperties(object obj)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in obj.GetType().GetProperties())
        {
            dict[prop.Name] = prop.GetValue(obj);
        }
        return dict;
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", null);
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
    }
}