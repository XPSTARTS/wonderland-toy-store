using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WonderlandBackend.Data;
using WonderlandBackend.DTOs;
using WonderlandBackend.Models;

namespace WonderlandBackend.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;

        public AuthService(
        ApplicationDbContext context,
        IJwtService jwtService,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor, 
        IMemoryCache cache,
        IEmailService emailService)
        {
            _context = context;
            _jwtService = jwtService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
            _emailService = emailService;
        }

        public async Task<User?> Register(RegisterDto registerDto)
        {
            if (_context.Users.Any(u => u.Email == registerDto.Email))
                return null;

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                FullName = registerDto.FullName,
                Role = "Customer",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var cart = new Cart
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<AuthResponseDto?> Login(LoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null)
                return null;

            bool isValidPassword = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
            if (!isValidPassword)
                return null;

            if (user.IsTwoFactorEnabled)
            {
                await SendTwoFactorCode(user.Email);
                return new AuthResponseDto
                {
                    RequiresTwoFactor = true,
                    Email = user.Email,
                    Message = "2FA code sent to your email"
                };
            }

            var response = await GenerateAuthResponse(user);

            if (_httpContextAccessor.HttpContext != null && response.AccessToken != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                };

                _httpContextAccessor.HttpContext.Response.Cookies.Append(
                    "WonderlandToken",
                    response.AccessToken,
                    cookieOptions
                );
            }

            return response;
        }

        public async Task<AuthResponseDto?> RefreshToken(RefreshTokenRequestDto request)
        {
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null)
                throw new SecurityTokenException("Invalid refresh token");

            if (refreshToken.IsUsed || refreshToken.IsRevoked)
                throw new SecurityTokenException("Refresh token has been revoked or used");

            if (refreshToken.ExpiryDate < DateTime.UtcNow)
                throw new SecurityTokenException("Refresh token has expired");

            if (refreshToken.User == null)
                throw new SecurityTokenException("User associated with refresh token not found");

            // Mark current token as used
            refreshToken.IsUsed = true;
            await _context.SaveChangesAsync();

            return await GenerateAuthResponse(refreshToken.User);
        }

        public async Task<bool> RevokeToken(RevokeTokenRequestDto request)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null)
                return false;

            if (refreshToken.IsRevoked)
                return true;

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedBy = GetClientIp();

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Logout(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Response.Cookies.Delete("WonderlandToken");
            }

            return true;
        }

        private async Task<AuthResponseDto> GenerateAuthResponse(User user)
        {
            var (accessToken, refreshToken) = _jwtService.GenerateTokens(user);

            try
            {
                // Create refresh token entity
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    JwtId = GetJwtIdFromToken(accessToken),
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    IsUsed = false,
                    IsRevoked = false,
                    CreatedAt = DateTime.UtcNow
                };

                // Log what we're about to save
                Console.WriteLine($"Saving refresh token for user: {user.Id}");
                Console.WriteLine($"Token: {refreshToken}");
                Console.WriteLine($"JwtId: {refreshTokenEntity.JwtId}");
                Console.WriteLine($"Expiry: {refreshTokenEntity.ExpiryDate}");

                _context.RefreshTokens.Add(refreshTokenEntity);

                // Try saving with explicit error handling
                try
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine("Refresh token saved successfully");
                }
                catch (DbUpdateException dbEx)
                {
                    Console.WriteLine($"DbUpdateException: {dbEx.Message}");
                    Console.WriteLine($"Inner: {dbEx.InnerException?.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving refresh token: {ex.Message}");
                throw;
            }

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(60),
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
            };
        }

        public async Task<TwoFactorResponseDto> ToggleTwoFactor(int userId, bool enable)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new TwoFactorResponseDto { Message = "User not found" };

            if (enable && !user.IsTwoFactorEnabled)
            {
                // Enable 2FA
                user.IsTwoFactorEnabled = true;
                user.TwoFactorEnabledAt = DateTime.UtcNow;

                // Generate recovery codes
                user.RecoveryCodes = GenerateRecoveryCodes();

                await _context.SaveChangesAsync();

                return new TwoFactorResponseDto
                {
                    IsEnabled = true,
                    Message = "2FA enabled successfully",
                    RecoveryCodes = user.RecoveryCodes
                };
            }
            else if (!enable && user.IsTwoFactorEnabled)
            {
                // Disable 2FA
                user.IsTwoFactorEnabled = false;
                user.TwoFactorSecret = null;
                user.RecoveryCodes = null;

                await _context.SaveChangesAsync();

                return new TwoFactorResponseDto
                {
                    IsEnabled = false,
                    Message = "2FA disabled successfully"
                };
            }

            return new TwoFactorResponseDto
            {
                IsEnabled = user.IsTwoFactorEnabled,
                Message = user.IsTwoFactorEnabled ? "2FA is already enabled" : "2FA is already disabled"
            };
        }

        // Generate and send 2FA code
        public async Task<bool> SendTwoFactorCode(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !user.IsTwoFactorEnabled)
                return false;

            // Generate 6-digit code
            var code = new Random().Next(100000, 999999).ToString();

            // Store in cache with 5-minute expiry
            _cache.Set($"2fa_{email}", code, TimeSpan.FromMinutes(5));

            // Send email
            await _emailService.SendTwoFactorCodeEmail(user.Email, user.FullName, code);

            return true;
        }

        // Verify 2FA code
        public async Task<AuthResponseDto?> VerifyTwoFactor(TwoFactorLoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !user.IsTwoFactorEnabled)
                return null;

            // Check cache for the code
            var cachedCode = _cache.Get<string>($"2fa_{dto.Email}");
            if (cachedCode == null || cachedCode != dto.Code)
                return null;

            // Clear the used code
            _cache.Remove($"2fa_{dto.Email}");

            // Also check if it's a recovery code
            if (user.RecoveryCodes != null && user.RecoveryCodes.Contains(dto.Code))
            {
                user.RecoveryCodes.Remove(dto.Code);
                await _context.SaveChangesAsync();
            }

            // Generate tokens
            return await GenerateAuthResponse(user);
        }

        // Generate recovery codes
        private List<string> GenerateRecoveryCodes()
        {
            var codes = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                codes.Add(Guid.NewGuid().ToString().Substring(0, 8).ToUpper());
            }
            return codes;
        }


        private string GetJwtIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.Id;
        }

        private string GetClientIp()
        {
            var context = _httpContextAccessor.HttpContext;
            var ip = context?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ip))
                ip = context?.Connection.RemoteIpAddress?.ToString();
            return ip ?? "unknown";
        }
    }
}