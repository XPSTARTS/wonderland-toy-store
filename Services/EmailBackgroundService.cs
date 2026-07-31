//using System.Collections.Concurrent;
//using WonderlandBackend.Models;

//namespace WonderlandBackend.Services
//{
//    public class EmailBackgroundService : BackgroundService
//    {
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ILogger<EmailBackgroundService> _logger;
//        private readonly ConcurrentQueue<EmailTask> _emailQueue = new();

//        public EmailBackgroundService(IServiceProvider serviceProvider, ILogger<EmailBackgroundService> logger)
//        {
//            _serviceProvider = serviceProvider;
//            _logger = logger;
//        }

//        public void QueueEmail(EmailTask task)
//        {
//            _emailQueue.Enqueue(task);
//            _logger.LogInformation($"📧 Email queued: {task.Type} for {task.ToEmail}");
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.LogInformation("🚀 EmailBackgroundService started and running!");

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    if (_emailQueue.TryDequeue(out var task))
//                    {
//                        _logger.LogInformation($"📧 Processing email: {task.Type} for {task.ToEmail}");

//                        using var scope = _serviceProvider.CreateScope();
//                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

//                        try
//                        {
//                            if (task.Type == "OrderConfirmation")
//                            {
//                                await emailService.SendOrderConfirmationEmail(
//                                    task.ToEmail,
//                                    task.CustomerName,
//                                    task.OrderId,
//                                    task.TotalAmount
//                                );
//                            }
//                            else if (task.Type == "AdminNotification")
//                            {
//                                await emailService.SendAdminNotificationEmail(
//                                    task.OrderId,
//                                    task.CustomerName,
//                                    task.CustomerEmail,
//                                    task.TotalAmount
//                                );
//                            }
//                            _logger.LogInformation($"✅ Email sent: {task.Type} to {task.ToEmail}");
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.LogError(ex, $"❌ Failed to send email: {task.Type} to {task.ToEmail}");
//                        }
//                    }
//                    else
//                    {
//                        await Task.Delay(100, stoppingToken);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Error in email background service");
//                    await Task.Delay(1000, stoppingToken);
//                }
//            }

//            _logger.LogInformation("📧 EmailBackgroundService stopped");
//        }
//    }
//}