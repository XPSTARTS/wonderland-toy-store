using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WonderlandBackend.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // ✅ Check if it's a rate limit exception by looking at the type name
                if (ex.GetType().Name == "QuotaExceededException")
                {
                    await HandleQuotaExceededException(context, ex);
                }
                else
                {
                    await HandleExceptionAsync(context, ex);
                }
            }
        }

        private async Task HandleQuotaExceededException(HttpContext context, Exception ex)
        {
            _logger.LogWarning($"Rate limit exceeded: {ex.Message}");

            // Try to get RetryAfter from the exception
            var retryAfter = "60";
            try
            {
                var retryProperty = ex.GetType().GetProperty("RetryAfter");
                if (retryProperty != null)
                {
                    var retryValue = retryProperty.GetValue(ex);
                    if (retryValue != null)
                    {
                        retryAfter = retryValue.ToString() ?? "60";
                    }
                }
            }
            catch
            {
                // Ignore - use default
            }

            var response = new
            {
                message = ex.Message ?? "Too many requests. Please try again later.",
                retryAfter = retryAfter,
                timestamp = DateTime.UtcNow
            };

            context.Response.StatusCode = 429;
            context.Response.Headers.Add("Retry-After", retryAfter);
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);

            var response = new
            {
                message = _env.IsDevelopment() ? ex.Message : "An error occurred while processing your request.",
                timestamp = DateTime.UtcNow
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}