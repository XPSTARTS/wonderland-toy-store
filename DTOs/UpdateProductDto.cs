using System.ComponentModel.DataAnnotations;

namespace WonderlandBackend.DTOs
{
    public class UpdateProductDto
    {
        [Required]
        [MinLength(3)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 999999)]
        public int StockQuantity { get; set; }
        public string? Category { get; set; }

        public string ImageUrl { get; set; } = string.Empty;
    }
}