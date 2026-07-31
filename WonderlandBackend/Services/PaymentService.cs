using WonderlandBackend.DTOs;
using Microsoft.Extensions.Logging;

namespace WonderlandBackend.Services
{
    public class PaymentService
    {
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(ILogger<PaymentService> logger)
        {
            _logger = logger;
        }

        public async Task<PaymentResponseDto> ProcessPayment(PaymentRequestDto request, decimal amount)
        {
            // ✅ Reduced delay for faster response
            await Task.Delay(10);

            _logger.LogInformation($"Processing payment for order {request.OrderId} using {request.PaymentMethod}");

            // Validate based on payment method
            if (request.PaymentMethod == "card" && request.CardDetails != null)
            {
                var validation = ValidateCard(request.CardDetails);
                if (!validation.IsValid)
                {
                    return new PaymentResponseDto
                    {
                        Success = false,
                        Message = validation.Message,
                        Status = "failed",
                        TransactionDate = DateTime.UtcNow,
                        Amount = amount,
                        PaymentMethod = request.PaymentMethod,
                        OrderStatus = "Payment Failed"
                    };
                }
            }

            // Generate fake transaction ID
            var transactionId = $"TXN-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            // Simulate success (90% success rate)
            var isSuccess = new Random().Next(1, 100) <= 90;

            if (isSuccess)
            {
                return new PaymentResponseDto
                {
                    Success = true,
                    TransactionId = transactionId,
                    Message = "Payment processed successfully!",
                    Status = "success",
                    TransactionDate = DateTime.UtcNow,
                    Amount = amount,
                    PaymentMethod = request.PaymentMethod,
                    OrderStatus = "Paid"
                };
            }

            return new PaymentResponseDto
            {
                Success = false,
                TransactionId = transactionId,
                Message = "Payment failed. Please try again.",
                Status = "failed",
                TransactionDate = DateTime.UtcNow,
                Amount = amount,
                PaymentMethod = request.PaymentMethod,
                OrderStatus = "Payment Failed"
            };
        }

        private (bool IsValid, string Message) ValidateCard(CardDetailsDto card)
        {
            if (string.IsNullOrEmpty(card.CardNumber) || card.CardNumber.Replace(" ", "").Length < 16)
                return (false, "Invalid card number");

            if (string.IsNullOrEmpty(card.ExpiryDate))
                return (false, "Invalid expiry date");

            if (string.IsNullOrEmpty(card.Cvv) || card.Cvv.Length < 3)
                return (false, "Invalid CVV");

            if (string.IsNullOrEmpty(card.CardHolderName))
                return (false, "Cardholder name is required");

            if (new Random().Next(1, 20) == 5)
                return (false, "Transaction declined by bank");

            return (true, "Card validated");
        }
    }
}