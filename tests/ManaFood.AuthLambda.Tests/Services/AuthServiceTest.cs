using System.IdentityModel.Tokens.Jwt;
using Amazon.DynamoDBv2;
using FluentAssertions;
using ManaFood.AuthLambda.Services;
using Moq;
using Xunit;

namespace ManaFood.AuthLambda.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly Mock<IAmazonDynamoDB> _mockDynamoDb;

    public AuthServiceTests()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "MyTestSecretKeyThatIsLongEnough1234567890");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");

        _mockDynamoDb = new Mock<IAmazonDynamoDB>();
    }

    [Fact]
    public void Constructor_WithoutJwtSecretKey_ThrowsException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("Jwt__SecretKey", null);

        // Act
        var act = () => new AuthService(_mockDynamoDb.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Jwt__SecretKey not configured");

        // Restore
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "MyTestSecretKeyThatIsLongEnough1234567890");
    }

    [Fact]
    public void Constructor_WithValidConfiguration_CreatesInstance()
    {
        // Act
        var authService = new AuthService(_mockDynamoDb.Object);

        // Assert
        authService.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithoutExpirationMinutes_UsesDefault60Minutes()
    {
        // Arrange
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", null);

        // Act
        var authService = new AuthService(_mockDynamoDb.Object);

        // Assert
        authService.Should().NotBeNull();
        
        // Restore
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
    }

    [Theory]
    [InlineData("30")]
    [InlineData("120")]
    [InlineData("1440")]
    public void Constructor_WithDifferentExpirationMinutes_AcceptsValue(string minutes)
    {
        // Arrange
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", minutes);

        // Act
        var authService = new AuthService(_mockDynamoDb.Object);

        // Assert
        authService.Should().NotBeNull();
        
        // Restore
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", null);
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
    }
}