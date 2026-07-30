using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WonderlandBackend.DTOs;
using WonderlandBackend.Services;

namespace WonderlandBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All cart endpoints require login
    public class CartController : ControllerBase
    {
        private readonly CartService _cartService;

        public CartController(CartService cartService)
        {
            _cartService = cartService;
        }

        // Helper method to get user ID from JWT token
        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("User ID not found in token");

            return int.Parse(userIdClaim.Value);
        }

        // GET: api/cart - Get user's cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userId = GetUserId();
                var cart = await _cartService.GetCart(userId);
                return Ok(cart);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid token" });
            }
        }

        // POST: api/cart/items - Add item to cart
        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto addDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var cart = await _cartService.AddToCart(userId, addDto);

                if (cart == null)
                    return BadRequest(new { message = "Product not found" });

                return Ok(cart);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/cart/items/{cartItemId} - Update cart item quantity
        [HttpPut("items/{cartItemId}")]
        public async Task<IActionResult> UpdateCartItem(int cartItemId, [FromBody] UpdateCartItemDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var cart = await _cartService.UpdateCartItem(userId, cartItemId, updateDto);

                if (cart == null)
                    return NotFound(new { message = "Cart item not found" });

                return Ok(cart);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/cart/items/{cartItemId} - Remove item from cart
        [HttpDelete("items/{cartItemId}")]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var userId = GetUserId();
            var cart = await _cartService.RemoveFromCart(userId, cartItemId);

            if (cart == null)
                return NotFound(new { message = "Cart item not found" });

            return Ok(cart);
        }

        // DELETE: api/cart/clear - Clear entire cart
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetUserId();
            var cart = await _cartService.ClearCart(userId);
            return Ok(cart);
        }
    }
}