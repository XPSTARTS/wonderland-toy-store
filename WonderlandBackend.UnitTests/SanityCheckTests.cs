using Xunit;
using Microsoft.EntityFrameworkCore;
using WonderlandBackend.Data;
using WonderlandBackend.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace WonderlandBackend.UnitTests;

public class SanityCheckTests
{
    [Fact]
    public void ProductService_CanBeInstantiated()
    {
        // 1. Setup a real in-memory database (This uses the package we just added)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        var context = new ApplicationDbContext(options);

        // 2. Mock the Redis cache service
        var mockCache = new Mock<IRedisCacheService>();

        // 3. Mock the Logger (Your service requires an ILogger!)
        var mockLogger = new Mock<ILogger<ProductService>>();

        // 4. Instantiate the service
        // This will throw an exception if the constructor signatures don't match
        var service = new ProductService(context, mockCache.Object, mockLogger.Object);

        // 5. If we reached this line, the test passed!
        Assert.NotNull(service);
    }
}