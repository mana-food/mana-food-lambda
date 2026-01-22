using FluentAssertions;
using ManaFood.AuthLambda.Services;
using Xunit;

namespace ManaFood.AuthLambda.Tests.Services;

public class UserInfoTests
{
    [Fact]
    public void UserInfo_DefaultConstructor_InitializesWithEmptyStrings()
    {
        // Act
        var user = new UserInfo();

        // Assert
        user.Id.Should().Be(string.Empty);
        user.Name.Should().Be(string.Empty);
        user.Email.Should().Be(string.Empty);
        user.Cpf.Should().Be(string.Empty);
        user.UserType.Should().Be(0);
    }

    [Fact]
    public void UserInfo_Properties_ShouldBeSettable()
    {
        // Arrange & Act
        var user = new UserInfo
        {
            Id = "user-123",
            Name = "João Silva",
            Email = "joao@test.com",
            Cpf = "12345678900",
            UserType = 1
        };

        // Assert
        user.Id.Should().Be("user-123");
        user.Name.Should().Be("João Silva");
        user.Email.Should().Be("joao@test.com");
        user.Cpf.Should().Be("12345678900");
        user.UserType.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void UserInfo_WithDifferentUserTypes_ShouldBeValid(int userType)
    {
        // Arrange & Act
        var user = new UserInfo { UserType = userType };

        // Assert
        user.UserType.Should().Be(userType);
    }
}