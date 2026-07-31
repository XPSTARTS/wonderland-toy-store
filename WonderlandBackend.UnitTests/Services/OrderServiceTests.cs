using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WonderlandBackend.Services;
using WonderlandBackend.Data;
using WonderlandBackend.DTOs;
using WonderlandBackend.Models;

namespace WonderlandBackend.UnitTests;

public class OrderServiceTests
{
    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"OrderTestDb_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateOrder_WithValidCart_CreatesOrderAndClearsCart()
    {
        // Arrange
        var context = CreateDbContext();

        var product = new Product { Id = 1, Name = "Test Product", Price = 10m, StockQuantity = 100 };
        context.Products.Add(product);

        var cart = new Cart { UserId = 100, CreatedAt = DateTime.UtcNow };
        context.Carts.Add(cart);
        context.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = 1, Quantity = 5 });
        await context.SaveChangesAsync();

        var realProductService = new ProductService(
            context,
            Mock.Of<IRedisCacheService>(),
            Mock.Of<ILogger<ProductService>>()
        );

        var realCartService = new CartService(context, realProductService);

        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<OrderService>>();

        var service = new OrderService(context, realCartService, realProductService,
            mockEmailService.Object, mockLogger.Object);

        var orderDto = new CreateOrderDto { ShippingAddress = "123 Test Street" };

        // Act
        var result = await service.CreateOrder(100, orderDto);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(50m);
        result.Status.Should().Be("Pending");

        var updatedProduct = await context.Products.FindAsync(1);
        updatedProduct!.StockQuantity.Should().Be(95);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyCart_ThrowsException()
    {
        // Arrange
        var context = CreateDbContext();

        var realProductService = new ProductService(
            context,
            Mock.Of<IRedisCacheService>(),
            Mock.Of<ILogger<ProductService>>()
        );

        var realCartService = new CartService(context, realProductService);

        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<OrderService>>();

        var service = new OrderService(context, realCartService, realProductService,
            mockEmailService.Object, mockLogger.Object);

        var orderDto = new CreateOrderDto { ShippingAddress = "123 Test Street" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.CreateOrder(100, orderDto));
    }

    [Fact]
    public async Task GetOrderById_AsUser_ReturnsOrder()
    {
        // Arrange
        var context = CreateDbContext();
        var order = new Order { Id = 1, UserId = 100, TotalAmount = 50m, Status = "Pending" };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var realProductService = new ProductService(
            context,
            Mock.Of<IRedisCacheService>(),
            Mock.Of<ILogger<ProductService>>()
        );

        var realCartService = new CartService(context, realProductService);
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<OrderService>>();

        var service = new OrderService(context, realCartService, realProductService,
            mockEmailService.Object, mockLogger.Object);

        // Act
        var result = await service.GetOrderById(100, 1);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.TotalAmount.Should().Be(50m);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithValidStatus_UpdatesSuccessfully()
    {
        // Arrange
        var context = CreateDbContext();
        var order = new Order { Id = 1, Status = "Pending" };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var realProductService = new ProductService(
            context,
            Mock.Of<IRedisCacheService>(),
            Mock.Of<ILogger<ProductService>>()
        );

        var realCartService = new CartService(context, realProductService);
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<OrderService>>();

        var service = new OrderService(context, realCartService, realProductService,
            mockEmailService.Object, mockLogger.Object);

        // Act
        var result = await service.UpdateOrderStatus(1, "Shipped");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Shipped");
    }

    [Fact]
    public async Task UpdateOrderStatus_WithInvalidStatus_ThrowsException()
    {
        // Arrange
        var context = CreateDbContext();
        var order = new Order { Id = 1, Status = "Pending" };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var realProductService = new ProductService(
            context,
            Mock.Of<IRedisCacheService>(),
            Mock.Of<ILogger<ProductService>>()
        );

        var realCartService = new CartService(context, realProductService);
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<OrderService>>();

        var service = new OrderService(context, realCartService, realProductService,
            mockEmailService.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.UpdateOrderStatus(1, "InvalidStatus"));
    }
}