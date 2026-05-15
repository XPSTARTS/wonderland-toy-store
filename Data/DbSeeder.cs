using WonderlandBackend.Models;

namespace WonderlandBackend.Data
{
    public static class DbSeeder
    {
        public static void SeedAdmin(ApplicationDbContext context)
        {
            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                var admin = new User
                {
                    Email = "Moid!12@wonderland.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!@"),
                    FullName = "Store Admin",
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(admin);
                context.SaveChanges();

                // Create cart for admin
                var cart = new Cart
                {
                    UserId = admin.Id,
                    CreatedAt = DateTime.UtcNow
                };
                context.Carts.Add(cart);
                context.SaveChanges();
            }
        }
    }
}