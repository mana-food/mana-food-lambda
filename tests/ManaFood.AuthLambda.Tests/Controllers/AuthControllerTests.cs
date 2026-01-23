using FluentAssertions;
using ManaFood.AuthLambda.Controllers;
using ManaFood.AuthLambda.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ManaFood.AuthLambda.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly Dictionary<string, string?> _originalEnvVars = new();

    public AuthControllerTests()
    {
        // Save original environment variables
        _originalEnvVars["AWS_REGION"] = Environment.GetEnvironmentVariable("AWS_REGION");
        _originalEnvVars["AWS_SERVICE_URL"] = Environment.GetEnvironmentVariable("AWS_SERVICE_URL");
        _originalEnvVars["Jwt__SecretKey"] = Environment.GetEnvironmentVariable("Jwt__SecretKey");
        _originalEnvVars["Jwt__Issuer"] = Environment.GetEnvironmentVariable("Jwt__Issuer");
        _originalEnvVars["Jwt__Audience"] = Environment.GetEnvironmentVariable("Jwt__Audience");
        _originalEnvVars["Jwt__ExpirationMinutes"] = Environment.GetEnvironmentVariable("Jwt__ExpirationMinutes");
    }

    public void Dispose()
    {
        // Restore original environment variables
        foreach (var kvp in _originalEnvVars)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }
    }

    private void SetupEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "MySecretKeyThatIsLongEnough1234567890");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "ManaFood");
        Environment.SetEnvironmentVariable("Jwt__Audience", "ManaFoodUsers");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
    }

    [Fact]
    public void Constructor_WithMissingJwtSecretKey_ThrowsInvalidOperationException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", null);

        // Act
        Action act = () => new AuthController();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Jwt__SecretKey not configured");
    }

    [Fact]
    public void Constructor_WithDefaultValues_CreatesController()
    {
        // Arrange
        SetupEnvironmentVariables();

        // Act
        var controller = new AuthController();

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomServiceUrl_CreatesController()
    {
        // Arrange
        SetupEnvironmentVariables();
        Environment.SetEnvironmentVariable("AWS_SERVICE_URL", "http://localhost:8000");

        // Act
        var controller = new AuthController();

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomRegion_CreatesController()
    {
        // Arrange
        SetupEnvironmentVariables();
        Environment.SetEnvironmentVariable("AWS_REGION", "us-west-2");

        // Act
        var controller = new AuthController();

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomJwtSettings_CreatesController()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "CustomSecretKeyThatIsLongEnough123");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "CustomIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "CustomAudience");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "120");

        // Act
        var controller = new AuthController();

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithDefaultJwtIssuer_CreatesController()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "MySecretKeyThatIsLongEnough1234567890");
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);

        // Act
        var controller = new AuthController();

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithDefaultJwtAudience_CreatesController()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "MySecretKeyThatIsLongEnough1234567890");
        Environment.SetEnvironmentVariable("Jwt__Audience", null);

        // Act
        var controller = new AuthController();

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithDefaultExpirationMinutes_CreatesController()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "MySecretKeyThatIsLongEnough1234567890");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", null);

        // Act
        var controller = new AuthController();

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        SetupEnvironmentVariables();
        var controller = new AuthController();

        // Act
        var result = await controller.Login(null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest!.Value.Should().BeEquivalentTo(new { message = "CPF and Password are required" });
    }

    [Theory]
    [InlineData(null, "password")]
    [InlineData("", "password")]
    [InlineData("   ", "password")]
    [InlineData("12345678900", null)]
    [InlineData("12345678900", "")]
    [InlineData("12345678900", "   ")]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest(string? cpf, string? password)
    {
        // Arrange
        SetupEnvironmentVariables();
        var controller = new AuthController();
        var request = new AuthRequest(cpf, password);

        // Act
        var result = await controller.Login(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest!.Value.Should().BeEquivalentTo(new { message = "CPF and Password are required" });
    }

    [Fact]
    public async Task Login_WhenDynamoDBThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        SetupEnvironmentVariables();
        Environment.SetEnvironmentVariable("AWS_SERVICE_URL", "http://localhost:9999");
        var controller = new AuthController();
        var request = new AuthRequest("12345678900", "password123");

        // Act
        var result = await controller.Login(request);

        // Assert
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(500);
    }
}