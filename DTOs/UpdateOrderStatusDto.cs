using System.ComponentModel.DataAnnotations;

namespace WonderlandBackend.DTOs
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}