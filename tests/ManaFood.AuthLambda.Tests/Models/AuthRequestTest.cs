using System.Text.Json;
using FluentAssertions;
using ManaFood.AuthLambda.Models;
using Xunit;

namespace ManaFood.AuthLambda.Tests.Models;

public class AuthRequestTests
{
    [Fact]
    public void AuthRequest_WithCpf_ShouldBeCreated()
    {
        // Arrange & Act
        var request = new AuthRequest("12345678900");

        // Assert
        request.Cpf.Should().Be("12345678900");
    }

    [Fact]
    public void AuthRequest_WithNullCpf_ShouldBeCreated()
    {
        // Arrange & Act
        var request = new AuthRequest(null);

        // Assert
        request.Cpf.Should().BeNull();
    }

    [Fact]
    public void AuthRequest_Deserialization_ShouldWork()
    {
        // Arrange
        var json = @"{""Cpf"":""12345678900""}";

        // Act
        var request = JsonSerializer.Deserialize<AuthRequest>(json);

        // Assert
        request.Should().NotBeNull();
        request!.Cpf.Should().Be("12345678900");
    }

    [Fact]
    public void AuthRequest_Deserialization_CamelCase_ShouldWork()
    {
        // Arrange
        var json = @"{""cpf"":""12345678900""}";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Act
        var request = JsonSerializer.Deserialize<AuthRequest>(json, options);

        // Assert
        request.Should().NotBeNull();
        request!.Cpf.Should().Be("12345678900");
    }

    [Fact]
    public void AuthRequest_Serialization_ShouldWork()
    {
        // Arrange
        var request = new AuthRequest("12345678900");

        // Act
        var json = JsonSerializer.Serialize(request);

        // Assert
        json.Should().Contain("12345678900");
        json.Should().Contain("Cpf"); 
    }

    [Theory]
    [InlineData("12345678900")]
    [InlineData("98765432100")]
    [InlineData("00011122233")]
    public void AuthRequest_WithDifferentCpfs_ShouldBeCreated(string cpf)
    {
        // Arrange & Act
        var request = new AuthRequest(cpf);

        // Assert
        request.Cpf.Should().Be(cpf);
    }

    [Fact]
    public void AuthRequest_RecordEquality_ShouldWork()
    {
        // Arrange
        var request1 = new AuthRequest("12345678900");
        var request2 = new AuthRequest("12345678900");
        var request3 = new AuthRequest("98765432100");

        // Assert
        request1.Should().Be(request2);
        request1.Should().NotBe(request3);
    }

    [Fact]
    public void AuthRequest_WithEmptyString_ShouldBeCreated()
    {
        // Arrange & Act
        var request = new AuthRequest("");

        // Assert
        request.Cpf.Should().BeEmpty();
    }

    [Fact]
    public void AuthRequest_WithWhiteSpace_ShouldBeCreated()
    {
        // Arrange & Act
        var request = new AuthRequest("   ");

        // Assert
        request.Cpf.Should().Be("   ");
    }
}