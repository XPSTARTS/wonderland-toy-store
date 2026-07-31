using Microsoft.EntityFrameworkCore;
using WonderlandBackend.Data;
using WonderlandBackend.Models;

namespace WonderlandBackend.UnitTests;

public static class MockDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);

        // Seed ONE test product matching your model exactly
        context.Products.Add(new Product
        {
            Id = 1,
            Name = "Test Product",
            Description = "Test Description",
            Price = 19.99m,
            StockQuantity = 100,
            ImageUrl = "test.jpg",
            Category = "Toys",
            CreatedAt = DateTime.UtcNow
        });

        context.SaveChanges();
        return context;
    }
}