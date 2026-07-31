namespace WonderlandBackend.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationEmail(string toEmail, string customerName, int orderId, decimal totalAmount);
        Task SendAdminNotificationEmail(int orderId, string customerName, string customerEmail, decimal totalAmount);
        Task SendTwoFactorCodeEmail(string toEmail, string customerName, string code);
        Task SendPasswordResetEmail(string toEmail, string customerName, string resetLink); // ✅ Add this
    }
}