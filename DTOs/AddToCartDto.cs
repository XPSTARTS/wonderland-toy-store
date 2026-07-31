using System.ComponentModel.DataAnnotations;

namespace WonderlandBackend.DTOs
{
    public class AddToCartDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 999)]
        public int Quantity { get; set; }
    }
}