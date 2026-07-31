using SendGrid;
using SendGrid.Helpers.Mail;
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

        // ✅ This method is kept but NOT called from OrderService anymore
        public async Task SendOrderConfirmationEmail(string toEmail, string customerName, int orderId, decimal totalAmount)
        {
            // This method is intentionally empty/disabled
            _logger.LogInformation($"📧 Customer email disabled for order #{orderId}");
            await Task.CompletedTask;
        }

        public async Task SendAdminNotificationEmail(int orderId, string customerName, string customerEmail, decimal totalAmount)
        {
            try
            {
                // ✅ FORCE admin email to your address
                var adminEmail = "abdulmoid47628@gmail.com";

                _logger.LogInformation($"📧 Sending admin notification to {adminEmail} for order #{orderId}");

                var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? _configuration["SendGrid:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("❌ SendGrid API key not configured");
                    return;
                }

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress("abzarizfinds@gmail.com", "Wonderland Toys");
                var to = new EmailAddress(adminEmail, "Store Admin");
                var subject = $"🔔 New Order! #{orderId}";

                var htmlContent = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .header {{ background: linear-gradient(135deg, #ef4444, #dc2626); color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 20px; }}
                            .order-details {{ background: #f9fafb; padding: 15px; border-radius: 8px; margin: 15px 0; }}
                            .highlight {{ color: #3b82f6; font-weight: bold; }}
                        </style>
                    </head>
                    <body>
                        <div class=""header"">
                            <h1>🚀 New Order Received!</h1>
                            <p>A new order has been placed on Wonderland Toys</p>
                        </div>
                        <div class=""content"">
                            <div class=""order-details"">
                                <p><strong>Order ID:</strong> #{orderId}</p>
                                <p><strong>Customer:</strong> {customerName}</p>
                                <p><strong>Email:</strong> {customerEmail}</p>
                                <p><strong>Total Amount:</strong> <span class=""highlight"">Rs {totalAmount:F2}</span></p>
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
                        <div class=""footer"">
                            <p>© 2024 Wonderland Toys. All rights reserved.</p>
                            <p>This is an automated notification for admin.</p>
                        </div>
                    </body>
                    </html>
                ";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await client.SendEmailAsync(msg);

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    _logger.LogInformation($"✅ Admin notification sent to {adminEmail}");
                }
                else
                {
                    var errorBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to send admin email. Status: {response.StatusCode}, Error: {errorBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to send admin notification email");
            }
        }

        public async Task SendTwoFactorCodeEmail(string toEmail, string customerName, string code)
        {
            try
            {
                _logger.LogInformation($"📧 Sending 2FA code to {toEmail}");

                var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? _configuration["SendGrid:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("❌ SendGrid API key not configured");
                    return;
                }

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress("abzarizfinds@gmail.com", "Wonderland Toys");
                var to = new EmailAddress(toEmail, customerName);
                var subject = "Your 2FA Verification Code";

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
                <div class=""header"">
                    <h1>🧸 Wonderland Toys</h1>
                    <p>Your 2FA Verification Code</p>
                </div>
                <div style=""padding: 20px;"">
                    <h2>Hello {customerName},</h2>
                    <p>Enter the following code to complete your login:</p>
                    <div class=""code"">{code}</div>
                    <p style=""margin-top: 20px; color: #6b7280;"">This code will expire in 5 minutes.</p>
                    <p>If you didn't request this code, please ignore this email.</p>
                </div>
                <div style=""background: #f3f4f6; padding: 10px; text-align: center; font-size: 12px; color: #6b7280;"">
                    <p>© 2024 Wonderland Toys. All rights reserved.</p>
                </div>
            </body>
            </html>
        ";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await client.SendEmailAsync(msg);

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    _logger.LogInformation($"✅ 2FA code sent to {toEmail}");
                }
                else
                {
                    var errorBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to send 2FA email. Status: {response.StatusCode}, Error: {errorBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to send 2FA code email to {toEmail}");
            }
        }

        public async Task SendPasswordResetEmail(string toEmail, string customerName, string resetLink)
        {
            try
            {
                _logger.LogInformation($"📧 Sending password reset email to {toEmail}");

                var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? _configuration["SendGrid:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("❌ SendGrid API key not configured");
                    return;
                }

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress("abzarizfinds@gmail.com", "Wonderland Toys");
                var to = new EmailAddress(toEmail, customerName);
                var subject = "Reset Your Password";

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
                <div class=""header"">
                    <h1>🧸 Wonderland Toys</h1>
                    <p>Password Reset Request</p>
                </div>
                <div style=""padding: 20px;"">
                    <h2>Hello {customerName},</h2>
                    <p>We received a request to reset your password. Click the button below to proceed:</p>
                    <p style=""text-align: center; margin: 30px 0;"">
                        <a href=""{resetLink}"" class=""button"">Reset Password</a>
                    </p>
                    <p style=""color: #6b7280; font-size: 14px;"">This link will expire in 24 hours.</p>
                    <p style=""color: #6b7280; font-size: 14px;"">If you didn't request this, please ignore this email.</p>
                </div>
                <div style=""background: #f3f4f6; padding: 10px; text-align: center; font-size: 12px; color: #6b7280;"">
                    <p>© 2024 Wonderland Toys. All rights reserved.</p>
                </div>
            </body>
            </html>
        ";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await client.SendEmailAsync(msg);

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    _logger.LogInformation($"✅ Password reset email sent to {toEmail}");
                }
                else
                {
                    var errorBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to send password reset email. Status: {response.StatusCode}, Error: {errorBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to send password reset email to {toEmail}");
            }
        }
    }
}