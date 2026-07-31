using Microsoft.EntityFrameworkCore;
using WonderlandBackend.Data;
using WonderlandBackend.DTOs;

namespace WonderlandBackend.Services
{
    public class AdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get dashboard statistics
        public async Task<AdminStatsDto> GetDashboardStats()
        {
            var totalProducts = await _context.Products.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalUsers = await _context.Users.CountAsync();

            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            var shippedOrders = await _context.Orders.CountAsync(o => o.Status == "Shipped");
            var deliveredOrders = await _context.Orders.CountAsync(o => o.Status == "Delivered");
            var cancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled");

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Delivered")
                .SumAsync(o => o.TotalAmount);

            // Get 10 most recent orders
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new RecentOrderDto
                {
                    Id = o.Id,
                    CustomerName = o.User != null ? o.User.FullName : "Unknown",
                    CustomerEmail = o.User != null ? o.User.Email : "Unknown",
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status
                })
                .ToListAsync();

            // Get products with low stock (less than 10 items)
            var lowStockProducts = await _context.Products
                .Where(p => p.StockQuantity < 10)
                .OrderBy(p => p.StockQuantity)
                .Select(p => new LowStockProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    StockQuantity = p.StockQuantity,
                    Price = p.Price
                })
                .ToListAsync();

            return new AdminStatsDto
            {
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalUsers = totalUsers,
                PendingOrders = pendingOrders,
                ShippedOrders = shippedOrders,
                DeliveredOrders = deliveredOrders,
                CancelledOrders = cancelledOrders,
                TotalRevenue = totalRevenue,
                RecentOrders = recentOrders,
                LowStockProducts = lowStockProducts
            };
        }

        // Get all users (Admin only)
        public async Task<List<UserDto>> GetAllUsers()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            }).ToList();
        }

        // Update user role (Admin only)
        public async Task<UserDto?> UpdateUserRole(int userId, string newRole)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            // Validate role
            if (newRole != "Admin" && newRole != "Customer")
                throw new Exception("Role must be either 'Admin' or 'Customer'");

            user.Role = newRole;
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
        }
    }
}