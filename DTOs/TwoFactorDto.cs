using System.ComponentModel.DataAnnotations;

namespace WonderlandBackend.DTOs
{
    public class EnableTwoFactorDto
    {
        public bool Enable { get; set; }
    }

    public class TwoFactorVerifyDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }

    public class TwoFactorLoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }

    public class TwoFactorResponseDto
    {
        public bool IsEnabled { get; set; }
        public string? Message { get; set; }
        public string? RecoveryCode { get; set; }
        public List<string>? RecoveryCodes { get; set; }
    }
}