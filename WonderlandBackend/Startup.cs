using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WonderlandBackend.Data;
using WonderlandBackend.Middleware;
using WonderlandBackend.Services;
using AspNetCoreRateLimit;
using Npgsql;

namespace WonderlandBackend;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Add services
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();

        // === Rate Limiting Configuration ===
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(options =>
        {
            options.GeneralRules = new List<RateLimitRule>
            {
                new RateLimitRule { Endpoint = "POST:/api/auth/login", Limit = 5, Period = "5m",
                    QuotaExceededResponse = new QuotaExceededResponse { Content = "{\"message\":\"Too many login attempts. Please try again in 5 minutes.\"}", ContentType = "application/json" } },
                new RateLimitRule { Endpoint = "POST:/api/auth/2fa/verify", Limit = 5, Period = "5m",
                    QuotaExceededResponse = new QuotaExceededResponse { Content = "{\"message\":\"Too many 2FA verification attempts. Please try again in 5 minutes.\"}", ContentType = "application/json" } },
                new RateLimitRule { Endpoint = "POST:/api/auth/2fa/send", Limit = 3, Period = "10m",
                    QuotaExceededResponse = new QuotaExceededResponse { Content = "{\"message\":\"Too many 2FA code requests. Please try again in 10 minutes.\"}", ContentType = "application/json" } },
                new RateLimitRule { Endpoint = "POST:/api/auth/register", Limit = 3, Period = "1h",
                    QuotaExceededResponse = new QuotaExceededResponse { Content = "{\"message\":\"Too many registration attempts. Please try again in 1 hour.\"}", ContentType = "application/json" } },
                new RateLimitRule { Endpoint = "GET:/api/products", Limit = 100, Period = "1m",
                    QuotaExceededResponse = new QuotaExceededResponse { Content = "{\"message\":\"Too many product requests. Please slow down.\"}", ContentType = "application/json" } },
                new RateLimitRule { Endpoint = "*", Limit = 60, Period = "1m",
                    QuotaExceededResponse = new QuotaExceededResponse { Content = "{\"message\":\"Too many requests. Please try again later.\"}", ContentType = "application/json" } }
            };
            options.EnableEndpointRateLimiting = true;
            options.StackBlockedRequests = false;
            options.RealIpHeader = "X-Real-IP";
            options.ClientIdHeader = "X-ClientId";
        });
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddInMemoryRateLimiting();

        // === Redis Caching ===
        services.AddStackExchangeRedisCache(options =>
        {
            var redisConnection = Environment.GetEnvironmentVariable("REDIS_URL") ?? "localhost:6379";
            options.Configuration = redisConnection;
            options.InstanceName = "Wonderland";
        });

        // === Swagger
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Wonderland Toy Store API", Version = "v1", Description = "E-commerce API for Wonderland Toy Store" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Enter 'Bearer' [space] and then your token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
            });
        });

        // === Database Context (WITH TIMEOUT FIX) ===
        var connectionString = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new Exception("SUPABASE_CONNECTION_STRING environment variable is not set.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.ConnectionStringBuilder.Timeout = 60; // ⏱️ 60 second timeout for cross-region lag
            dataSourceBuilder.ConnectionStringBuilder.CommandTimeout = 60;
            options.UseNpgsql(dataSourceBuilder.Build());
        });

        // === JWT Authentication (PRODUCTION SAFE) ===
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? Configuration.GetValue<string>("Jwt:Key");
        var jwtIssuer = "wonderland-backend";
        var jwtAudience = "wonderland-frontend";

        if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
        {
            throw new Exception("JWT_KEY must be at least 32 characters long and properly configured in Environment Variables.");
        }

        var key = Encoding.ASCII.GetBytes(jwtKey);

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Name = "WonderlandToken";
        });

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var cookie = context.Request.Cookies["WonderlandToken"];
                    if (!string.IsNullOrEmpty(cookie)) context.Token = cookie;
                    return Task.CompletedTask;
                }
            };
        });

        // Register Services
        services.AddScoped<AuthService>();
        services.AddScoped<ProductService>();
        services.AddScoped<CartService>();
        services.AddScoped<OrderService>();
        services.AddScoped<AdminService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IRedisCacheService, RedisCacheService>();

        // ============================================================
        // ✅ FIXED CORS FOR PRODUCTION (Reading from Environment Variable)
        // ============================================================
        var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            ?? new[] { "https://wonderland-toys.vercel.app", "http://localhost:5173" };

        services.AddCors(options =>
        {
            options.AddPolicy("ProductionPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials(); 
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseCors("ProductionPolicy");

        app.UseIpRateLimiting();

        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<GlobalExceptionMiddleware>();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wonderland Toy Store API v1");
            c.RoutePrefix = "swagger";
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}