using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WonderlandBackend.DTOs;
using WonderlandBackend.Services;

namespace WonderlandBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService)
        {
            _productService = productService;
        }

        // GET: api/products - Public (no auth required)
        [HttpGet]
        public async Task<IActionResult> GetAllProducts([FromQuery] ProductPaginationDto pagination)
        {
            var result = await _productService.GetProductsPaginated(pagination);
            return Ok(result);
        }


        // GET: api/products/5 - Public (no auth required)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productService.GetProductById(id);

            if (product == null)
                return NotFound(new { message = "Product not found" });

            return Ok(product);
        }

        // POST: api/products - Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _productService.CreateProduct(createDto);
            return Ok(new { message = "Product created successfully", product });
        }

        // PUT: api/products/5 - Admin only
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _productService.UpdateProduct(id, updateDto);

            if (product == null)
                return NotFound(new { message = "Product not found" });

            return Ok(new { message = "Product updated successfully", product });
        }

        // DELETE: api/products/5 - Admin only
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProduct(id);

            if (!result)
                return NotFound(new { message = "Product not found" });

            return Ok(new { message = "Product deleted successfully" });
        }
    }
}