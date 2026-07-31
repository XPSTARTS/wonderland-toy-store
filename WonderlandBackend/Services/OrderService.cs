using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WonderlandBackend.Data;
using WonderlandBackend.DTOs;
using WonderlandBackend.Models;

namespace WonderlandBackend.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private readonly ProductService _productService;
        private readonly IEmailService _emailService;
        private readonly ILogger<OrderService> _logger;

        // ✅ ADD THIS INTERFACE TO YOUR CONSTRUCTOR
        private readonly IDbContextTransaction? _transaction;
        public OrderService(
            ApplicationDbContext context,
            CartService cartService,
            ProductService productService,
            IEmailService emailService,
            ILogger<OrderService> logger)
        {
            _context = context;
            _cartService = cartService;
            _productService = productService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<OrderDto?> CreateOrder(int userId, CreateOrderDto orderDto)
        {
            // ✅ START TRANSACTION SAFELY
            IDbContextTransaction? transaction = null;
            if (_context.Database.IsRelational())
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                // Get user's cart items
                var cartItems = await _cartService.GetCartItemsForCheckout(userId);

                if (cartItems == null || !cartItems.Any())
                    throw new Exception("Cart is empty");

                // Check stock availability
                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        throw new Exception($"Product not found: {item.ProductId}");

                    if (product.StockQuantity < item.Quantity)
                        throw new Exception($"Insufficient stock for {product.Name}");
                }

                // Calculate total amount
                decimal totalAmount = cartItems.Sum(item => (item.Product?.Price ?? 0) * item.Quantity);

                // Create order
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    ShippingAddress = orderDto.ShippingAddress ?? "No address provided",
                    PaymentMethod = null,
                    TransactionId = null,
                    PaymentStatus = "Unpaid",
                    PaymentDate = null
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order items
                foreach (var cartItem in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.Product?.Price ?? 0
                    };
                    _context.OrderItems.Add(orderItem);

                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= cartItem.Quantity;
                    }
                }

                await _context.SaveChangesAsync();
                await _cartService.ClearCart(userId);

                // ✅ COMMIT SAFELY
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                _logger.LogInformation($"✅ Order {order.Id} completed successfully");

                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    _logger.LogInformation($"📧 Sending admin notification for order {order.Id}");

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendAdminNotificationEmail(
                                order.Id,
                                user.FullName,
                                user.Email,
                                order.TotalAmount
                            );
                            _logger.LogInformation($"✅ Admin email sent for order {order.Id}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"❌ Admin email failed for order {order.Id}");
                        }
                    });
                }
                else
                {
                    _logger.LogWarning($"⚠️ User {userId} not found for order {order.Id}");
                }

                return await GetOrderById(userId, order.Id);
            }
            catch (Exception ex)
            {
                // ✅ ROLLBACK SAFELY
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }

                _logger.LogError(ex, $"❌ Order creation failed for user {userId}");
                throw;
            }
        }

        // Get order by ID (with authorization check)
        public async Task<OrderDto?> GetOrderById(int userId, int orderId, bool isAdmin = false)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.Id == orderId);

            if (!isAdmin)
                query = query.Where(o => o.UserId == userId);

            var order = await query.FirstOrDefaultAsync();

            if (order == null)
                return null;

            return new OrderDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                ShippingAddress = order.ShippingAddress,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "Unknown Product",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.UnitPrice * oi.Quantity
                }).ToList()
            };
        }

        // Get user's order history
        public async Task<List<OrderDto>> GetUserOrders(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(order => new OrderDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                ShippingAddress = order.ShippingAddress,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "Unknown Product",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.UnitPrice * oi.Quantity
                }).ToList()
            }).ToList();
        }

        // Get all orders (Admin only)
        public async Task<List<OrderDto>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(order => new OrderDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                ShippingAddress = order.ShippingAddress,
                CustomerName = order.User?.FullName,
                CustomerEmail = order.User?.Email,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "Unknown Product",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.UnitPrice * oi.Quantity
                }).ToList()
            }).ToList();
        }

        // Update order status (Admin only)
        public async Task<OrderDto?> UpdateOrderStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
                return null;

            // Validate status
            var validStatuses = new[] { "Pending", "Shipped", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(newStatus))
                throw new Exception("Invalid status. Allowed: Pending, Shipped, Delivered, Cancelled");

            order.Status = newStatus;
            await _context.SaveChangesAsync();

            // Get full order details with items
            var orderDetails = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (orderDetails == null)
                return null;

            return new OrderDto
            {
                Id = orderDetails.Id,
                OrderDate = orderDetails.OrderDate,
                TotalAmount = orderDetails.TotalAmount,
                Status = orderDetails.Status,
                ShippingAddress = orderDetails.ShippingAddress,
                Items = orderDetails.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "Unknown Product",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.UnitPrice * oi.Quantity
                }).ToList()
            };
        }

        // Update order payment (for payment processing)
        public async Task<bool> UpdateOrderPayment(int orderId, string paymentMethod, string transactionId, string paymentStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return false;

            order.PaymentMethod = paymentMethod;
            order.TransactionId = transactionId;
            order.PaymentStatus = paymentStatus;
            order.PaymentDate = DateTime.UtcNow;

            // If payment is successful, update order status
            if (paymentStatus == "Paid")
            {
                order.Status = "Processing";
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}