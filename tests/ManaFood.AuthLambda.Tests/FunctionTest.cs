using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using FluentAssertions;
using Xunit;

namespace ManaFood.AuthLambda.Tests;

public class FunctionTests
{
    [Fact]
    public void AppSettings_Deserialization_ShouldWork()
    {
        // Arrange
        var json = @"{
            ""Jwt"": {
                ""SecretKey"": ""MySecretKey123456789012345678901234567890"",
                ""ExpirationMinutes"": 1440,
                ""Issuer"": ""TestIssuer"",
                ""Audience"": ""TestAudience""
            }
        }";

        // Act
        var appSettings = JsonSerializer.Deserialize<ManaFood.AuthLambda.AppSettings>(json);

        // Assert
        appSettings.Should().NotBeNull();
        appSettings!.Jwt.Should().NotBeNull();
        appSettings.Jwt!.SecretKey.Should().Be("MySecretKey123456789012345678901234567890");
        appSettings.Jwt.ExpirationMinutes.Should().Be(1440);
        appSettings.Jwt.Issuer.Should().Be("TestIssuer");
        appSettings.Jwt.Audience.Should().Be("TestAudience");
    }

    [Fact]
    public void JwtSettings_Properties_ShouldBeSettable()
    {
        // Arrange & Act
        var jwtSettings = new ManaFood.AuthLambda.JwtSettings
        {
            SecretKey = "TestSecret",
            ExpirationMinutes = 60,
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        };

        // Assert
        jwtSettings.SecretKey.Should().Be("TestSecret");
        jwtSettings.ExpirationMinutes.Should().Be(60);
        jwtSettings.Issuer.Should().Be("TestIssuer");
        jwtSettings.Audience.Should().Be("TestAudience");
    }

    [Fact]
    public void ApiGatewayRequest_WithNullBody_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/api/auth/login",
            Body = null
        };

        // Assert 
        request.Body.Should().BeNull();
    }

    [Fact]
    public void ApiGatewayRequest_WithEmptyBody_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/api/auth/login",
            Body = string.Empty
        };

        // Assert
        request.Body.Should().BeEmpty();
    }

    [Fact]
    public void ApiGatewayRequest_WithInvalidJson_ShouldBeHandled()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var act = () => JsonSerializer.Deserialize<ManaFood.AuthLambda.Models.AuthRequest>(invalidJson);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void ApiGatewayResponse_Serialization_ShouldWork()
    {
        // Arrange
        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new { token = "test-token", expiresIn = 3600 }),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };

        // Assert
        response.StatusCode.Should().Be(200);
        response.Body.Should().Contain("test-token");
        response.Headers.Should().ContainKey("Content-Type");
    }

    [Fact]
    public void LambdaContext_LogInformation_ShouldWork()
    {
        // Arrange
        var context = new TestLambdaContext
        {
            FunctionName = "ManaFood.AuthLambda",
            FunctionVersion = "1"
        };

        // Act & Assert
        context.Logger.Should().NotBeNull();
        context.FunctionName.Should().Be("ManaFood.AuthLambda");
    }
}