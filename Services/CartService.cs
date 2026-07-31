using Microsoft.EntityFrameworkCore;
using WonderlandBackend.Data;
using WonderlandBackend.DTOs;
using WonderlandBackend.Models;

namespace WonderlandBackend.Services
{
    public class CartService
    {
        private readonly ApplicationDbContext _context;
        private readonly ProductService _productService;

        public CartService(ApplicationDbContext context, ProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        // Get user's cart
        public async Task<CartDto> GetCart(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                // Create cart if doesn't exist
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartItems = cart.CartItems.Select(ci => new CartItemDto
            {
                Id = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name ?? "Unknown Product",
                ProductPrice = ci.Product?.Price ?? 0,
                Quantity = ci.Quantity,
                Subtotal = (ci.Product?.Price ?? 0) * ci.Quantity
            }).ToList();

            return new CartDto
            {
                Id = cart.Id,
                Items = cartItems,
                TotalAmount = cartItems.Sum(i => i.Subtotal),
                TotalItems = cartItems.Sum(i => i.Quantity)
            };
        }

        // Add item to cart
        public async Task<CartDto?> AddToCart(int userId, AddToCartDto addDto)
        {
            // Check if product exists and has stock
            var product = await _context.Products.FindAsync(addDto.ProductId);
            if (product == null)
                return null;

            if (product.StockQuantity < addDto.Quantity)
                throw new Exception($"Only {product.StockQuantity} items available in stock");

            // Get user's cart
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Check if product already in cart
            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == addDto.ProductId);

            if (existingItem != null)
            {
                // Update quantity
                int newQuantity = existingItem.Quantity + addDto.Quantity;
                if (product.StockQuantity < newQuantity)
                    throw new Exception($"Only {product.StockQuantity} items available in stock");

                existingItem.Quantity = newQuantity;
            }
            else
            {
                // Add new item
                cart.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = addDto.ProductId,
                    Quantity = addDto.Quantity
                });
            }

            await _context.SaveChangesAsync();
            return await GetCart(userId);
        }

        // Update cart item quantity
        public async Task<CartDto?> UpdateCartItem(int userId, int cartItemId, UpdateCartItemDto updateDto)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return null;

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
            if (cartItem == null)
                return null;

            // Check stock
            var product = await _context.Products.FindAsync(cartItem.ProductId);
            if (product != null && product.StockQuantity < updateDto.Quantity)
                throw new Exception($"Only {product.StockQuantity} items available in stock");

            if (updateDto.Quantity <= 0)
            {
                // Remove item if quantity is 0 or negative
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity = updateDto.Quantity;
            }

            await _context.SaveChangesAsync();
            return await GetCart(userId);
        }

        // Remove item from cart
        public async Task<CartDto?> RemoveFromCart(int userId, int cartItemId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return null;

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
            if (cartItem == null)
                return null;

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            return await GetCart(userId);
        }

        // Clear entire cart
        public async Task<CartDto?> ClearCart(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return null;

            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();
            return await GetCart(userId);
        }

        // Get cart items for checkout (returns items with product details)
        public async Task<List<CartItem>> GetCartItemsForCheckout(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return cart?.CartItems.ToList() ?? new List<CartItem>();
        }
    }
}