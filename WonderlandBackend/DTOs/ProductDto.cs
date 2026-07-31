using System.ComponentModel.DataAnnotations;

namespace WonderlandBackend.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
        public string? Category { get; set; } 
        public string ImageUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}