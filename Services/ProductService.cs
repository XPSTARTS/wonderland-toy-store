using Microsoft.EntityFrameworkCore;
using WonderlandBackend.Data;
using WonderlandBackend.DTOs;
using WonderlandBackend.Models;

namespace WonderlandBackend.Services
{
    public class ProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRedisCacheService _cache;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            ApplicationDbContext context,
            IRedisCacheService cache,
            ILogger<ProductService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // Get all products (public) - WITH CACHING
        public async Task<List<ProductDto>> GetAllProducts()
        {
            const string cacheKey = "products_all";

            var cachedProducts = await _cache.GetAsync<List<ProductDto>>(cacheKey);
            if (cachedProducts != null && cachedProducts.Any())
            {
                _logger.LogInformation("Products retrieved from cache");
                return cachedProducts;
            }

            _logger.LogInformation("Products retrieved from database");

            var products = await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var result = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Category = p.Category ?? "Uncategorized",
                CreatedAt = p.CreatedAt
            }).ToList();

            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }

        // Get single product by id (public) - WITH CACHING
        public async Task<ProductDto?> GetProductById(int id)
        {
            var cacheKey = $"product_{id}";

            var cachedProduct = await _cache.GetAsync<ProductDto>(cacheKey);
            if (cachedProduct != null)
            {
                _logger.LogInformation($"📦 Product {id} retrieved from cache");
                return cachedProduct;
            }

            _logger.LogInformation($"📦 Product {id} retrieved from database");

            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return null;

            var result = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                Category = product.Category ?? "Uncategorized",
                CreatedAt = product.CreatedAt
            };

            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }

        // Get products with pagination - WITH CACHING
        public async Task<PagedResponseDto<ProductDto>> GetProductsPaginated(ProductPaginationDto pagination)
        {
            // ✅ Create cache key based on query parameters
            var cacheKey = $"products_page_{pagination.Page}_{pagination.PageSize}_{pagination.Search ?? "null"}_{pagination.Category ?? "null"}_{pagination.SortBy ?? "null"}";

            // ✅ Try to get from cache
            var cachedResult = await _cache.GetAsync<PagedResponseDto<ProductDto>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation($"📦 Products page {pagination.Page} retrieved from cache");
                return cachedResult;
            }

            _logger.LogInformation($"📦 Products page {pagination.Page} retrieved from database");

            var query = _context.Products.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(pagination.Search))
            {
                query = query.Where(p => p.Name.Contains(pagination.Search) ||
                                         p.Description.Contains(pagination.Search));
            }

            // Apply category filter
            if (!string.IsNullOrEmpty(pagination.Category) && pagination.Category != "All")
            {
                query = query.Where(p => p.Category != null && p.Category == pagination.Category);
            }

            // Apply sorting
            query = pagination.SortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    ImageUrl = p.ImageUrl,
                    Category = p.Category ?? "Uncategorized",
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            var result = new PagedResponseDto<ProductDto>
            {
                Items = items,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
            };

            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(3));

            return result;
        }

        // Create product (Admin only) - CLEAR CACHE
        public async Task<ProductDto> CreateProduct(CreateProductDto createDto)
        {
            var product = new Product
            {
                Name = createDto.Name,
                Description = createDto.Description,
                Price = createDto.Price,
                StockQuantity = createDto.StockQuantity,
                ImageUrl = createDto.ImageUrl,
                Category = createDto.Category ?? "Uncategorized",
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await ClearProductCache();

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                Category = product.Category,
                CreatedAt = product.CreatedAt
            };
        }

        // Update product (Admin only) - CLEAR CACHE
        public async Task<ProductDto?> UpdateProduct(int id, UpdateProductDto updateDto)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return null;

            product.Name = updateDto.Name;
            product.Description = updateDto.Description;
            product.Price = updateDto.Price;
            product.StockQuantity = updateDto.StockQuantity;
            product.ImageUrl = updateDto.ImageUrl;
            product.Category = updateDto.Category ?? "Uncategorized";

            await _context.SaveChangesAsync();

            await ClearProductCache();
            await _cache.RemoveAsync($"product_{id}");

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                Category = product.Category,
                CreatedAt = product.CreatedAt
            };
        }

        // Delete product (Admin only) - CLEAR CACHE
        public async Task<bool> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            await ClearProductCache();
            await _cache.RemoveAsync($"product_{id}");

            return true;
        }

        // Check if product exists and has stock
        public async Task<bool> CheckStock(int productId, int requestedQuantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return false;

            return product.StockQuantity >= requestedQuantity;
        }

        // Update stock (used when placing orders)
        public async Task<bool> UpdateStock(int productId, int quantityToReduce)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return false;

            if (product.StockQuantity < quantityToReduce)
                return false;

            product.StockQuantity -= quantityToReduce;
            await _context.SaveChangesAsync();

            // ✅ Clear product cache after stock update
            await _cache.RemoveAsync($"product_{productId}");
            await ClearProductCache();

            return true;
        }

        private async Task ClearProductCache()
        {
            await _cache.RemoveAsync("products_all");
        }
    }
}