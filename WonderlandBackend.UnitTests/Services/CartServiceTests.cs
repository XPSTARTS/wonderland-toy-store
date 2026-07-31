using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WonderlandBackend.Data;
using WonderlandBackend.DTOs;
using WonderlandBackend.Models;
using WonderlandBackend.Services;
using Xunit;

namespace WonderlandBackend.UnitTests;

public class CartServiceTests
{
    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"CartTestDb_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddToCart_NewItem_AddsToCartSuccessfully()
    {
        // Arrange
        var context = CreateDbContext();

        // Seed a product
        context.Products.Add(new Product
        {
            Id = 1,
            Name = "Test Product",
            Price = 19.99m,
            StockQuantity = 100
        });
        await context.SaveChangesAsync();

        // Mock the ProductService (which is a dependency of CartService)
        var mockProductService = new Mock<ProductService>(
            context,
            Mock.Of<IRedisCacheService>(),
            Mock.Of<ILogger<ProductService>>()
        );

        var service = new CartService(context, mockProductService.Object);

        var addDto = new AddToCartDto { ProductId = 1, Quantity = 5 };

        // Act
        var result = await service.AddToCart(100, addDto); // 100 = new UserId

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().ProductId.Should().Be(1);
        result.Items.First().Quantity.Should().Be(5);
        result.TotalItems.Should().Be(5);
        result.TotalAmount.Should().Be(19.99m * 5);
    }

    [Fact]
    public async Task AddToCart_ExistingItem_IncreasesQuantity()
    {
        // Arrange
        var context = CreateDbContext();
        context.Products.Add(new Product { Id = 1, Name = "Test Product", Price = 10m, StockQuantity = 100 });

        var cart = new Cart { UserId = 100, CreatedAt = DateTime.UtcNow };
        context.Carts.Add(cart);
        context.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = 1, Quantity = 2 });
        await context.SaveChangesAsync();

        var mockProductService = new Mock<ProductService>(
            context,
            Mock.Of<IRedisCacheService>(),
            Mock.Of<ILogger<ProductService>>()
        );

        var service = new CartService(context, mockProductService.Object);
        var addDto = new AddToCartDto { ProductId = 1, Quantity = 3 };

        // Act
        var result = await service.AddToCart(100, addDto);

        // Assert
        result.Should().NotBeNull();
        result.Items.First().Quantity.Should().Be(5); // 2 + 3 = 5
    }

    [Fact]
    public async Task RemoveFromCart_RemovesItemSuccessfully()
    {
        // Arrange
        var context = CreateDbContext();
        context.Products.Add(new Product { Id = 1, Name = "Test Product", Price = 10m, StockQuantity = 100 });

        var cart = new Cart { UserId = 100, CreatedAt = DateTime.UtcNow };
        context.Carts.Add(cart);
        var cartItem = new CartItem { CartId = cart.Id, ProductId = 1, Quantity = 2 };
        context.CartItems.Add(cartItem);
        await context.SaveChangesAsync();

        var mockProductService = new Mock<ProductService>(
            context,
            Mock.Of<IRedisCacheService>(),
            Mock.Of<ILogger<ProductService>>()
        );

        var service = new CartService(context, mockProductService.Object);

        // Act
        var result = await service.RemoveFromCart(100, cartItem.Id);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
    }
}