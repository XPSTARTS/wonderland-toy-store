using Microsoft.EntityFrameworkCore;
using WonderlandBackend;
using WonderlandBackend.Data;
using WonderlandBackend.Services;

var builder = WebApplication.CreateBuilder(args);
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, app.Environment);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Ensures the database is created if it doesn't exist (which it already does)
        dbContext.Database.EnsureCreated();
        Console.WriteLine("✅ Database checked successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Database check failed (probably due to timeout), but continuing anyway: {ex.Message}");
    }

    // Seed the Admin user
    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@wonderland.com";
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin123!";
    var adminName = Environment.GetEnvironmentVariable("ADMIN_NAME") ?? "Store Admin";

    DbSeeder.SeedAdmin(dbContext, adminEmail, adminPassword, adminName);
}

app.Run();