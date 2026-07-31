using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using WonderlandBackend.Services;
using WonderlandBackend.DTOs;

namespace WonderlandBackend.UnitTests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetProductById_WhenExists_ReturnsProductDto()
    {
        // 1. Arrange
        var context = MockDbContextFactory.Create();
        var mockCache = new Mock<IRedisCacheService>();
        var mockLogger = new Mock<ILogger<ProductService>>();

        var service = new ProductService(context, mockCache.Object, mockLogger.Object);

        // 2. Act
        var result = await service.GetProductById(1);

        // 3. Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Product");
        result.Price.Should().Be(19.99m);
    }

    [Fact]
    public async Task GetProductById_WhenNotExists_ReturnsNull()
    {
        // 1. Arrange
        var context = MockDbContextFactory.Create();
        var mockCache = new Mock<IRedisCacheService>();
        var mockLogger = new Mock<ILogger<ProductService>>();

        var service = new ProductService(context, mockCache.Object, mockLogger.Object);

        // 2. Act
        var result = await service.GetProductById(999); // ID that doesn't exist

        // 3. Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateProduct_AddsProductToDatabase_AndClearsCache()
    {
        // 1. Arrange
        var context = MockDbContextFactory.Create();
        var mockCache = new Mock<IRedisCacheService>();
        var mockLogger = new Mock<ILogger<ProductService>>();

        var service = new ProductService(context, mockCache.Object, mockLogger.Object);

        var newProduct = new CreateProductDto
        {
            Name = "New Toy",
            Description = "A brand new toy",
            Price = 29.99m,
            StockQuantity = 50,
            ImageUrl = "newtoy.jpg",
            Category = "Toys"
        };

        // 2. Act
        var result = await service.CreateProduct(newProduct);

        // 3. Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Toy");
        result.Price.Should().Be(29.99m);

        // Verify it was added to the database
        var savedProduct = await context.Products.FindAsync(result.Id);
        savedProduct.Should().NotBeNull();
        savedProduct!.Name.Should().Be("New Toy");

        // Verify cache was cleared because we called CreateProduct
        mockCache.Verify(c => c.RemoveAsync("products_all"), Times.Once);
    }

    [Fact]
    public async Task GetAllProducts_ReturnsCachedData_WhenCacheExists()
    {
        // 1. Arrange
        var context = MockDbContextFactory.Create();
        var mockCache = new Mock<IRedisCacheService>();
        var mockLogger = new Mock<ILogger<ProductService>>();

        var cachedList = new List<ProductDto>
        {
            new ProductDto { Id = 99, Name = "Cached Product", Price = 9.99m }
        };

        // Setup the mock to return cached data
        mockCache.Setup(c => c.GetAsync<List<ProductDto>>("products_all"))
                 .ReturnsAsync(cachedList);

        var service = new ProductService(context, mockCache.Object, mockLogger.Object);

        // 2. Act
        var result = await service.GetAllProducts();

        // 3. Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Cached Product");

        // Verify it did NOT hit the database (it used cache instead)
        mockCache.Verify(c => c.GetAsync<List<ProductDto>>("products_all"), Times.Once);
    }
}