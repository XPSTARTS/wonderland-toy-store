using System.ComponentModel.DataAnnotations;

namespace WonderlandBackend.DTOs
{
    public class CreateOrderDto
    {
        [Required]
        [MinLength(5)]
        public string ShippingAddress { get; set; } = string.Empty;
    }
}