using Amazon.DynamoDBv2;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using ManaFood.AuthLambda.Controllers;
using ManaFood.AuthLambda.Models;
using ManaFood.AuthLambda.Services;
using Moq;
using Xunit;

namespace ManaFood.AuthLambda.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly Mock<IAmazonDynamoDB> _mockDynamoDb;
    private readonly AuthService _authService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "MyTestSecretKeyThatIsLongEnough1234567890");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");

        _mockDynamoDb = new Mock<IAmazonDynamoDB>();
        _authService = new AuthService(_mockDynamoDb.Object);
        _controller = new AuthController(_authService);
    }

    [Theory]
    [InlineData(null, "senha123")]
    [InlineData("", "senha123")]
    [InlineData("   ", "senha123")]
    [InlineData("12345678900", null)]
    [InlineData("12345678900", "")]
    [InlineData("12345678900", "   ")]
    public async Task Login_WithMissingFields_ReturnsBadRequest(string? cpf, string? password)
    {
        // Arrange
        var request = new AuthRequest(cpf, password);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var value = badRequestResult.Value;
        value.Should().BeEquivalentTo(new { message = "CPF and Password are required" });
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", null);
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
    }
}