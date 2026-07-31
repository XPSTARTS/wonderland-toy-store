using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WonderlandBackend.Services;
using WonderlandBackend.Models;
using System.Collections.Generic;

namespace WonderlandBackend.UnitTests;

public class JwtServiceTests
{
    private IJwtService CreateJwtService()
    {
        // ✅ HARDCODED CONFIGURATION - Use string? to fix nullability warning
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "EreWGJiE4xmiyMaAiQjxYFMdDr6FrJWX" },
            { "Jwt:ExpiryInMinutes", "60" },
            { "Jwt:RefreshTokenExpiryDays", "7" }
        };

        // ✅ Build a real IConfiguration object
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        return new JwtService(configuration);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidToken()
    {
        var service = CreateJwtService();
        var user = new User { Id = 1, Email = "test@test.com", FullName = "Test User", Role = "Customer" };

        var token = service.GenerateAccessToken(user);

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var service = CreateJwtService();

        var token = service.GenerateRefreshToken();

        token.Should().NotBeNullOrEmpty();
        token.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public void GenerateTokens_ReturnsBothTokens()
    {
        var service = CreateJwtService();
        var user = new User { Id = 1, Email = "test@test.com", FullName = "Test User", Role = "Customer" };

        var (accessToken, refreshToken) = service.GenerateTokens(user);

        accessToken.Should().NotBeNullOrEmpty();
        refreshToken.Should().NotBeNullOrEmpty();
    }
}