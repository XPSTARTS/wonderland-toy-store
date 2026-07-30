namespace WonderlandBackend.DTOs
{
    public class PaymentRequestDto
    {
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // "card", "cod", "bank"
        public CardDetailsDto? CardDetails { get; set; }
        public string? UpiId { get; set; }
    }

    public class CardDetailsDto
    {
        public string CardNumber { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
    }

    public class PaymentResponseDto
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "success", "failed", "pending"
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? OrderStatus { get; set; }
    }
}