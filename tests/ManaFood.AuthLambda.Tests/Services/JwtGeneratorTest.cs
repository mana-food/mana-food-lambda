using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using ManaFood.AuthLambda.Services;
using Xunit;

namespace ManaFood.AuthLambda.Tests.Services;

public class JwtGeneratorTests
{
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var generator = new JwtGenerator(
            "MySecretKeyThatIsLongEnough1234567890",
            "TestIssuer",
            "TestAudience",
            60
        );

        // Assert
        generator.Should().NotBeNull();
    }

    [Fact]
    public void GenerateToken_WithValidUser_ReturnsTokenAndExpiresIn()
    {
        // Arrange
        var generator = new JwtGenerator(
            "MySecretKeyThatIsLongEnough1234567890",
            "TestIssuer",
            "TestAudience",
            60
        );

        var user = new UserInfo
        {
            Id = "user-123",
            Name = "João Silva",
            Email = "joao@test.com",
            Cpf = "12345678900",
            UserType = 1
        };

        // Act
        var (token, expiresIn) = generator.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        expiresIn.Should().Be(3600);
        
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_TokenContainsExpectedClaims()
    {
        // Arrange
        var generator = new JwtGenerator(
            "MySecretKeyThatIsLongEnough1234567890",
            "TestIssuer",
            "TestAudience",
            60
        );

        var user = new UserInfo
        {
            Id = "user-123",
            Name = "João Silva",
            Email = "joao@test.com",
            Cpf = "12345678900",
            UserType = 1
        };

        // Act
        var (token, _) = generator.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "user-123");
        jwtToken.Claims.Should().Contain(c => c.Type == "name" && c.Value == "João Silva");
        jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == "joao@test.com");
        jwtToken.Claims.Should().Contain(c => c.Type == "cpf" && c.Value == "12345678900");
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "CUSTOMER");
        jwtToken.Claims.Should().Contain(c => c.Type == "jti");
    }

    [Theory]
    [InlineData(0, "ADMIN")]
    [InlineData(1, "CUSTOMER")]
    [InlineData(2, "KITCHEN")]
    [InlineData(3, "OPERATOR")]
    [InlineData(4, "MANAGER")]
    [InlineData(99, "CUSTOMER")]
    public void GenerateToken_WithDifferentUserTypes_ReturnsCorrectRole(int userType, string expectedRole)
    {
        // Arrange
        var generator = new JwtGenerator(
            "MySecretKeyThatIsLongEnough1234567890",
            "TestIssuer",
            "TestAudience",
            60
        );

        var user = new UserInfo
        {
            Id = "user-123",
            Name = "Test User",
            Email = "test@test.com",
            Cpf = "12345678900",
            UserType = userType
        };

        // Act
        var (token, _) = generator.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
        
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(expectedRole);
    }

    [Fact]
    public void GenerateToken_TokenHasCorrectIssuerAndAudience()
    {
        // Arrange
        var generator = new JwtGenerator(
            "MySecretKeyThatIsLongEnough1234567890",
            "MyTestIssuer",
            "MyTestAudience",
            60
        );

        var user = new UserInfo
        {
            Id = "user-123",
            Name = "Test",
            Email = "test@test.com",
            Cpf = "12345678900",
            UserType = 1
        };

        // Act
        var (token, _) = generator.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Issuer.Should().Be("MyTestIssuer");
        jwtToken.Audiences.Should().Contain("MyTestAudience");
    }
}