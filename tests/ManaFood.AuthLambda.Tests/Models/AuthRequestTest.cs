using System.Text.Json;
using FluentAssertions;
using ManaFood.AuthLambda.Models;
using Xunit;

namespace ManaFood.AuthLambda.Tests.Models;

public class AuthRequestTests
{
    [Fact]
    public void AuthRequest_WithCpfAndPassword_ShouldBeCreated()
    {
        // Arrange & Act
        var request = new AuthRequest("12345678900", "senha123");

        // Assert
        request.Cpf.Should().Be("12345678900");
        request.Password.Should().Be("senha123");
    }

    [Fact]
    public void AuthRequest_WithNullValues_ShouldBeCreated()
    {
        // Arrange & Act
        var request = new AuthRequest(null, null);

        // Assert
        request.Cpf.Should().BeNull();
        request.Password.Should().BeNull();
    }

    [Fact]
    public void AuthRequest_Deserialization_PascalCase_ShouldWork()
    {
        // Arrange
        var json = @"{""Cpf"":""12345678900"",""Password"":""senha123""}";

        // Act
        var request = JsonSerializer.Deserialize<AuthRequest>(json);

        // Assert
        request.Should().NotBeNull();
        request!.Cpf.Should().Be("12345678900");
        request.Password.Should().Be("senha123");
    }

    [Fact]
    public void AuthRequest_Deserialization_CamelCase_ShouldWork()
    {
        // Arrange
        var json = @"{""cpf"":""12345678900"",""password"":""senha123""}";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Act
        var request = JsonSerializer.Deserialize<AuthRequest>(json, options);

        // Assert
        request.Should().NotBeNull();
        request!.Cpf.Should().Be("12345678900");
        request.Password.Should().Be("senha123");
    }

    [Fact]
    public void AuthRequest_Serialization_ShouldWork()
    {
        // Arrange
        var request = new AuthRequest("12345678900", "senha123");

        // Act
        var json = JsonSerializer.Serialize(request);

        // Assert
        json.Should().Contain("12345678900");
        json.Should().Contain("senha123");
        json.Should().Contain("Cpf");
        json.Should().Contain("Password");
    }

    [Fact]
    public void AuthRequest_RecordEquality_ShouldWork()
    {
        // Arrange
        var request1 = new AuthRequest("12345678900", "senha123");
        var request2 = new AuthRequest("12345678900", "senha123");
        var request3 = new AuthRequest("98765432100", "outrasenha");

        // Assert
        request1.Should().Be(request2);
        request1.Should().NotBe(request3);
    }

    [Theory]
    [InlineData("", "senha123")]
    [InlineData("   ", "senha123")]
    [InlineData("12345678900", "")]
    [InlineData("12345678900", "   ")]
    public void AuthRequest_WithEmptyOrWhitespace_ShouldBeCreated(string? cpf, string? password)
    {
        // Arrange & Act
        var request = new AuthRequest(cpf, password);

        // Assert
        request.Cpf.Should().Be(cpf);
        request.Password.Should().Be(password);
    }
}