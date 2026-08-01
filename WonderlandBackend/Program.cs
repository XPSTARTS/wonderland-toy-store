using WonderlandBackend;
using WonderlandBackend.Data;
using WonderlandBackend.Services;

var builder = WebApplication.CreateBuilder(args);

try
{
    Console.WriteLine("✅ Starting Wonderland Backend...");
    var startup = new Startup(builder.Configuration);
    startup.ConfigureServices(builder.Services);

    var app = builder.Build();
    startup.Configure(app, app.Environment);

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            dbContext.Database.EnsureCreated();
            Console.WriteLine("✅ Database check passed (EnsureCreated).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Database check failed (EnsureCreated), skipping: {ex.Message}");
            Console.WriteLine("⚠️ Your database might already exist, or you need to run migrations manually.");
        }

        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@wonderland.com";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin123!";
        var adminName = Environment.GetEnvironmentVariable("ADMIN_NAME") ?? "Store Admin";

        try
        {
            DbSeeder.SeedAdmin(dbContext, adminEmail, adminPassword, adminName);
            Console.WriteLine("✅ Admin seeded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Admin seeding failed (might already exist): {ex.Message}");
        }
    }

    Console.WriteLine("✅ Backend started successfully!");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("❌ CRITICAL STARTUP ERROR:");
    Console.WriteLine(ex.ToString());
    throw;
}