using System.Text.Json;
using FluentAssertions;
using ManaFood.AuthLambda.Services;
using Xunit;

namespace ManaFood.AuthLambda.Tests.Services;

public class SecretsManagerServiceTests
{
    [Fact]
    public void DatabaseSecret_Deserialization_ShouldWork()
    {
        // Arrange
        var json = @"{
            ""username"": ""admin"",
            ""password"": ""password123"",
            ""host"": ""localhost"",
            ""port"": 3306,
            ""dbClusterIdentifier"": ""test-cluster""
        }";

        // Act
        var secret = JsonSerializer.Deserialize<DatabaseSecret>(json);

        // Assert
        secret.Should().NotBeNull();
        secret!.Username.Should().Be("admin");
        secret.Password.Should().Be("password123");
        secret.Host.Should().Be("localhost");
        secret.Port.Should().Be(3306);
        secret.DbClusterIdentifier.Should().Be("test-cluster");
    }

    [Fact]
    public void DatabaseSecret_WithMissingPort_ShouldDeserialize()
    {
        // Arrange
        var json = @"{
            ""username"": ""admin"",
            ""password"": ""password123"",
            ""host"": ""localhost"",
            ""dbClusterIdentifier"": ""test-cluster""
        }";

        // Act
        var secret = JsonSerializer.Deserialize<DatabaseSecret>(json);

        // Assert
        secret.Should().NotBeNull();
        secret!.Port.Should().Be(0);
    }

    [Fact]
    public void DatabaseCredentials_ToConnectionString_ShouldFormatCorrectly()
    {
        // Arrange
        var credentials = new DatabaseCredentials
        {
            Host = "localhost",
            Port = 3306,
            Database = "testdb",
            Username = "admin",
            Password = "password123"
        };

        // Act
        var connectionString = credentials.ToConnectionString();

        // Assert
        connectionString.Should().Contain("Server=localhost");
        connectionString.Should().Contain("Port=3306");
        connectionString.Should().Contain("Database=testdb");
        connectionString.Should().Contain("Uid=admin");
        connectionString.Should().Contain("Pwd=password123");
        connectionString.Should().Contain("SslMode=Required");
    }

    [Fact]
    public void DatabaseCredentials_Properties_ShouldBeSettable()
    {
        // Arrange & Act
        var credentials = new DatabaseCredentials
        {
            Host = "testhost",
            Port = 5432,
            Database = "testdb",
            Username = "testuser",
            Password = "testpass"
        };

        // Assert
        credentials.Host.Should().Be("testhost");
        credentials.Port.Should().Be(5432);
        credentials.Database.Should().Be("testdb");
        credentials.Username.Should().Be("testuser");
        credentials.Password.Should().Be("testpass");
    }

    [Fact]
    public void DatabaseCredentials_DefaultConstructor_ShouldInitializeWithEmptyStrings()
    {
        // Act
        var credentials = new DatabaseCredentials();

        // Assert
        credentials.Host.Should().Be(string.Empty);
        credentials.Database.Should().Be(string.Empty);
        credentials.Username.Should().Be(string.Empty);
        credentials.Password.Should().Be(string.Empty);
        credentials.Port.Should().Be(0);
    }
}