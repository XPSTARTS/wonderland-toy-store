using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using WonderlandBackend.Services;
using WonderlandBackend.Data;
using WonderlandBackend.DTOs;
using WonderlandBackend.Models;

namespace WonderlandBackend.UnitTests;

public class AuthServiceTests
{
    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"AuthTestDb_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Register_WithNewEmail_CreatesUserAndCart()
    {
        // Arrange
        var context = CreateDbContext();
        var mockJwt = new Mock<IJwtService>();
        var mockConfig = new Mock<IConfiguration>();
        var mockHttp = new Mock<IHttpContextAccessor>();
        var mockCache = new Mock<IMemoryCache>();
        var mockEmail = new Mock<IEmailService>();

        var service = new AuthService(
            context, mockJwt.Object, mockConfig.Object,
            mockHttp.Object, mockCache.Object, mockEmail.Object);

        var registerDto = new RegisterDto
        {
            Email = "newuser@example.com",
            Password = "TestPassword123!",
            FullName = "Test User"
        };

        // Act
        var result = await service.Register(registerDto);

        // Assert
        result.Should().NotBeNull();
        result?.Email.Should().Be("newuser@example.com");
        result?.FullName.Should().Be("Test User");
        result?.Role.Should().Be("Customer");

        // Verify Cart was automatically created
        var cart = await context.Carts.FirstOrDefaultAsync(c => c.UserId == result.Id);
        cart.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsNull()
    {
        // Arrange
        var context = CreateDbContext();
        context.Users.Add(new User
        {
            Email = "existing@example.com",
            PasswordHash = "hashed",
            FullName = "Existing User"
        });
        await context.SaveChangesAsync();

        var mockJwt = new Mock<IJwtService>();
        var mockConfig = new Mock<IConfiguration>();
        var mockHttp = new Mock<IHttpContextAccessor>();
        var mockCache = new Mock<IMemoryCache>();
        var mockEmail = new Mock<IEmailService>();

        var service = new AuthService(
            context, mockJwt.Object, mockConfig.Object,
            mockHttp.Object, mockCache.Object, mockEmail.Object);

        var registerDto = new RegisterDto
        {
            Email = "existing@example.com",
            Password = "TestPassword123!",
            FullName = "Test User"
        };

        // Act
        var result = await service.Register(registerDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var context = CreateDbContext();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = passwordHash,
            FullName = "Test User",
            Role = "Customer"
        });
        await context.SaveChangesAsync();

        string jwtId = "test-jwt-id-123";
        string fakeAccessToken = $"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJ7dGVzdC1qd3QtaWQtMTIzfSJ9.fakesignature";
        string fakeRefreshToken = "fake_refresh_token_123";

        var mockJwt = new Mock<IJwtService>();
        mockJwt.Setup(j => j.GenerateTokens(It.IsAny<User>()))
               .Returns((fakeAccessToken, fakeRefreshToken));

        var mockConfig = new Mock<IConfiguration>();
        var mockHttp = new Mock<IHttpContextAccessor>();
        var mockCache = new Mock<IMemoryCache>();
        var mockEmail = new Mock<IEmailService>();

        var service = new AuthService(
            context, mockJwt.Object, mockConfig.Object,
            mockHttp.Object, mockCache.Object, mockEmail.Object);

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "CorrectPassword123!"
        };

        // Act
        var result = await service.Login(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(fakeAccessToken);
        result.RefreshToken.Should().Be(fakeRefreshToken);
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var context = CreateDbContext();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        context.Users.Add(new User
        {
            Email = "test@example.com",
            PasswordHash = passwordHash,
            FullName = "Test User"
        });
        await context.SaveChangesAsync();

        var mockJwt = new Mock<IJwtService>();
        var mockConfig = new Mock<IConfiguration>();
        var mockHttp = new Mock<IHttpContextAccessor>();
        var mockCache = new Mock<IMemoryCache>();
        var mockEmail = new Mock<IEmailService>();

        var service = new AuthService(
            context, mockJwt.Object, mockConfig.Object,
            mockHttp.Object, mockCache.Object, mockEmail.Object);

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var result = await service.Login(loginDto);

        // Assert
        result.Should().BeNull();
    }
}