using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WonderlandBackend.DTOs;
using WonderlandBackend.Services;

namespace WonderlandBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _paymentService; // ✅ Changed from IPaymentService
        private readonly OrderService _orderService;

        public PaymentController(PaymentService paymentService, OrderService orderService)
        {
            _paymentService = paymentService;
            _orderService = orderService;
        }

        [HttpPost("process/{orderId}")]
        public async Task<IActionResult> ProcessPayment(int orderId, [FromBody] PaymentRequestDto paymentRequest)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var isAdmin = User.IsInRole("Admin");

                var order = await _orderService.GetOrderById(userId, orderId, isAdmin);
                if (order == null)
                    return NotFound(new { message = "Order not found" });

                var result = await _paymentService.ProcessPayment(paymentRequest, order.TotalAmount);

                if (result.Success)
                {
                    await _orderService.UpdateOrderPayment(
                        orderId,
                        paymentRequest.PaymentMethod,
                        result.TransactionId,
                        "Paid"
                    );
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}