using System.ComponentModel.DataAnnotations;

namespace WonderlandBackend.DTOs
{
    public class UpdateCartItemDto
    {
        [Required]
        [Range(1, 999)]
        public int Quantity { get; set; }
    }
}