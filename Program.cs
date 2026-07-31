using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WonderlandBackend.Data;
using WonderlandBackend.Middleware;
using WonderlandBackend.Services;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// === Rate Limiting Configuration ===
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        // Auth endpoints - Strict limits
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/login",
            Limit = 5,
            Period = "5m",
            QuotaExceededResponse = new QuotaExceededResponse
            {
                Content = "{\"message\":\"Too many login attempts. Please try again in 5 minutes.\"}",
                ContentType = "application/json"
            }
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/2fa/verify",
            Limit = 5,
            Period = "5m",
            QuotaExceededResponse = new QuotaExceededResponse
            {
                Content = "{\"message\":\"Too many 2FA verification attempts. Please try again in 5 minutes.\"}",
                ContentType = "application/json"
            }
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/2fa/send",
            Limit = 3,
            Period = "10m",
            QuotaExceededResponse = new QuotaExceededResponse
            {
                Content = "{\"message\":\"Too many 2FA code requests. Please try again in 10 minutes.\"}",
                ContentType = "application/json"
            }
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/register",
            Limit = 3,
            Period = "1h",
            QuotaExceededResponse = new QuotaExceededResponse
            {
                Content = "{\"message\":\"Too many registration attempts. Please try again in 1 hour.\"}",
                ContentType = "application/json"
            }
        },
        new RateLimitRule
        {
            Endpoint = "GET:/api/products",
            Limit = 100,
            Period = "1m",
            QuotaExceededResponse = new QuotaExceededResponse
            {
                Content = "{\"message\":\"Too many product requests. Please slow down.\"}",
                ContentType = "application/json"
            }
        },
        // General rules - Standard limits
        new RateLimitRule
        {
            Endpoint = "*",
            Limit = 60,
            Period = "1m",
            QuotaExceededResponse = new QuotaExceededResponse
            {
                Content = "{\"message\":\"Too many requests. Please try again later.\"}",
                ContentType = "application/json"
            }
        }
    };
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
});
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddInMemoryRateLimiting();

// === Redis Caching (for 2FA + general distributed cache) ===
builder.Services.AddStackExchangeRedisCache(options =>
{
    var redisConnection = Environment.GetEnvironmentVariable("REDIS_URL") ?? "localhost:6379";
    options.Configuration = redisConnection;
    options.InstanceName = "Wonderland";
});

// === Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Wonderland Toy Store API",
        Version = "v1",
        Description = "E-commerce API for Wonderland Toy Store"
    });
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
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database Context
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
Console.WriteLine("=== Database Configuration ===");
if (!string.IsNullOrEmpty(databaseUrl))
{
    Console.WriteLine("DATABASE_URL found!");
    Console.WriteLine("USING Aiven POSTGRESQL");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(databaseUrl));
}
else
{
    Console.WriteLine("⚠️ DATABASE_URL not found, falling back to local MS SQL");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}
Console.WriteLine("=============================");

// JWT Authentication - Read from environment variables
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration.GetValue<string>("Jwt:Key");
var jwtIssuer = "wonderland-backend";
var jwtAudience = "wonderland-frontend";

if (!string.IsNullOrEmpty(jwtKey))
{
    jwtKey = jwtKey.Trim();
    if (jwtKey.Length < 32)
    {
        Console.WriteLine($"⚠️ JWT_KEY is too short ({jwtKey.Length} chars). Using fallback.");
        jwtKey = "EreWGJiE4xmiyMaAiQjxYFMdDr6FrJWX";
    }
}

if (string.IsNullOrEmpty(jwtKey))
    throw new Exception("JWT_KEY environment variable not configured");

Console.WriteLine($"🔑 JWT_KEY length: {jwtKey.Length} characters");

var key = Encoding.ASCII.GetBytes(jwtKey);

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "WonderlandToken";
});

builder.Services.AddAuthentication(options =>
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
            if (!string.IsNullOrEmpty(cookie))
            {
                context.Token = cookie;
            }
            return Task.CompletedTask;
        }
    };
});

// Register Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ✅ Apply CORS first
app.UseCors("AllowAll");

// ✅ Apply Rate Limiting
app.UseIpRateLimiting();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// === Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wonderland Toy Store API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Create database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();

    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@wonderland.com";
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin123!";
    var adminName = Environment.GetEnvironmentVariable("ADMIN_NAME") ?? "Store Admin";

    DbSeeder.SeedAdmin(dbContext, adminEmail, adminPassword, adminName);
}

app.Run();