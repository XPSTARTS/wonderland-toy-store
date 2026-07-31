using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WonderlandBackend.DTOs;

namespace WonderlandBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // ✅ Universal method that handles ALL email sends
        private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                // Connect to Gmail SMTP
                await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                // Authenticate using your App Password
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"✅ Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to send email to {toEmail}");
                throw; // Re-throw so the calling service knows it failed
            }
        }

        // ✅ 2FA Code Email
        public async Task SendTwoFactorCodeEmail(string toEmail, string customerName, string code)
        {
            var subject = "Your 2FA Verification Code - Wonderland Toys";
            var htmlContent = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .header {{ background: linear-gradient(135deg, #3b82f6, #8b5cf6); color: white; padding: 20px; text-align: center; }}
                        .code {{ font-size: 32px; font-weight: bold; color: #3b82f6; padding: 15px; background: #f3f4f6; border-radius: 8px; letter-spacing: 4px; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>🧸 Wonderland Toys</h1>
                        <p>Your 2FA Verification Code</p>
                    </div>
                    <div style='padding: 20px;'>
                        <h2>Hello {customerName},</h2>
                        <p>Enter the following code to complete your login:</p>
                        <div class='code'>{code}</div>
                        <p style='margin-top: 20px; color: #6b7280;'>This code will expire in 5 minutes.</p>
                        <p>If you didn't request this code, please ignore this email.</p>
                    </div>
                    <div style='background: #f3f4f6; padding: 10px; text-align: center; font-size: 12px; color: #6b7280;'>
                        <p>© 2024 Wonderland Toys. All rights reserved.</p>
                    </div>
                </body>
                </html>
            ";
            await SendEmailAsync(toEmail, customerName, subject, htmlContent);
        }

        // ✅ Admin Notification Email (New Order)
        public async Task SendAdminNotificationEmail(int orderId, string customerName, string customerEmail, decimal totalAmount)
        {
            var subject = $"🔔 New Order! #{orderId} - Wonderland Toys";
            var htmlContent = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .header {{ background: linear-gradient(135deg, #ef4444, #dc2626); color: white; padding: 20px; text-align: center; }}
                        .order-details {{ background: #f9fafb; padding: 15px; border-radius: 8px; margin: 15px 0; }}
                        .highlight {{ color: #3b82f6; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>🚀 New Order Received!</h1>
                        <p>A new order has been placed on Wonderland Toys</p>
                    </div>
                    <div style='padding: 20px;'>
                        <div class='order-details'>
                            <p><strong>Order ID:</strong> #{orderId}</p>
                            <p><strong>Customer:</strong> {customerName}</p>
                            <p><strong>Email:</strong> {customerEmail}</p>
                            <p><strong>Total Amount:</strong> <span class='highlight'>Rs {totalAmount:F2}</span></p>
                            <p><strong>Status:</strong> Pending</p>
                        </div>
                        <p><strong>Action Required:</strong></p>
                        <ul>
                            <li>Verify payment status</li>
                            <li>Process the order</li>
                            <li>Update shipping details</li>
                        </ul>
                        <p>View in admin panel to process.</p>
                    </div>
                    <div style='background: #f9fafb; padding: 10px; text-align: center; font-size: 12px; color: #6b7280;'>
                        <p>© 2024 Wonderland Toys. All rights reserved.</p>
                        <p>This is an automated notification for admin.</p>
                    </div>
                </body>
                </html>
            ";
            // Send admin email to YOUR email address (hardcoded)
            await SendEmailAsync("abdulmoid47628@gmail.com", "Store Admin", subject, htmlContent);
        }

        // ✅ Password Reset Email
        public async Task SendPasswordResetEmail(string toEmail, string customerName, string resetLink)
        {
            var subject = "Reset Your Password - Wonderland Toys";
            var htmlContent = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .header {{ background: linear-gradient(135deg, #3b82f6, #8b5cf6); color: white; padding: 20px; text-align: center; }}
                        .button {{ display: inline-block; background: #3b82f6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>🧸 Wonderland Toys</h1>
                        <p>Password Reset Request</p>
                    </div>
                    <div style='padding: 20px;'>
                        <h2>Hello {customerName},</h2>
                        <p>We received a request to reset your password. Click the button below to proceed:</p>
                        <p style='text-align: center; margin: 30px 0;'>
                            <a href='{resetLink}' class='button'>Reset Password</a>
                        </p>
                        <p style='color: #6b7280; font-size: 14px;'>This link will expire in 24 hours.</p>
                        <p style='color: #6b7280; font-size: 14px;'>If you didn't request this, please ignore this email.</p>
                    </div>
                    <div style='background: #f3f4f6; padding: 10px; text-align: center; font-size: 12px; color: #6b7280;'>
                        <p>© 2024 Wonderland Toys. All rights reserved.</p>
                    </div>
                </body>
                </html>
            ";
            await SendEmailAsync(toEmail, customerName, subject, htmlContent);
        }

        // ✅ Customer Order Confirmation Email (Optional, for future use)
        public async Task SendOrderConfirmationEmail(string toEmail, string customerName, int orderId, decimal totalAmount)
        {
            var subject = $"Order Confirmation #{orderId} - Wonderland Toys";
            var htmlContent = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .header {{ background: linear-gradient(135deg, #10b981, #059669); color: white; padding: 20px; text-align: center; }}
                        .highlight {{ color: #3b82f6; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>🎉 Thank You for Your Order!</h1>
                        <p>Order #{orderId} has been placed successfully</p>
                    </div>
                    <div style='padding: 20px;'>
                        <h2>Hello {customerName},</h2>
                        <p>Your order has been received and is being processed.</p>
                        <p><strong>Total Amount:</strong> <span class='highlight'>Rs {totalAmount:F2}</span></p>
                        <p>You will receive a notification when your order ships.</p>
                    </div>
                </body>
                </html>
            ";
            await SendEmailAsync(toEmail, customerName, subject, htmlContent);
        }
    }
}