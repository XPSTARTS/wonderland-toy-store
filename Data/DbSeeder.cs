// Services/DbSeeder.cs
using WonderlandBackend.Data;
using WonderlandBackend.Models;

namespace WonderlandBackend.Services
{
    public static class DbSeeder
    {
        public static void SeedAdmin(ApplicationDbContext context, string email, string password, string name)
        {
            // Check if any admin exists
            var adminExists = context.Users.Any(u => u.Role == "Admin");
            if (adminExists)
                return;

            var admin = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = name,
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(admin);
            context.SaveChanges();
        }
    }
}