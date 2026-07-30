using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WonderlandBackend.DTOs;
using WonderlandBackend.Services;

namespace WonderlandBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrdersController(OrderService orderService)
        {
            _orderService = orderService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("User ID not found in token");

            return int.Parse(userIdClaim.Value);
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        // POST: api/orders - Create order from cart
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto orderDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var order = await _orderService.CreateOrder(userId, orderDto);
                return Ok(new { message = "Order placed successfully!", order });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/orders - Get user's order history
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();
            var orders = await _orderService.GetUserOrders(userId);
            return Ok(orders);
        }

        // GET: api/orders/{id} - Get specific order
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var userId = GetUserId();
            var isAdmin = IsAdmin();

            var order = await _orderService.GetOrderById(userId, id, isAdmin);

            if (order == null)
                return NotFound(new { message = "Order not found" });

            return Ok(order);
        }

        // GET: api/orders/admin/all - Get all orders (Admin only)
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrders();
            return Ok(orders);
        }

        // PUT: api/orders/{id}/status - Update order status (Admin only)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto statusDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var order = await _orderService.UpdateOrderStatus(id, statusDto.Status);

                if (order == null)
                    return NotFound(new { message = "Order not found" });

                return Ok(new { message = "Order status updated successfully", order });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}