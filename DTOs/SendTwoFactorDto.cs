using System.ComponentModel.DataAnnotations;

namespace WonderlandBackend.DTOs
{
    public class SendTwoFactorDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}