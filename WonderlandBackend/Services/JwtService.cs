using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WonderlandBackend.Models;

namespace WonderlandBackend.Services
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        (string accessToken, string refreshToken) GenerateTokens(User user);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly int _accessTokenExpiryMinutes;
        private readonly int _refreshTokenExpiryDays;
        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
            _accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");
            _refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");

            // ✅ Log the key being used (for debugging)
            var key = GetJwtKey();
            Console.WriteLine($"🔑 JWT_KEY used for signing: {key.Substring(0, Math.Min(10, key.Length))}...");
        }
        private string GetJwtKey()
        {
            var key = Environment.GetEnvironmentVariable("JWT_KEY") ?? _configuration.GetValue<string>("Jwt:Key");
            if (string.IsNullOrEmpty(key))
                throw new Exception("JWT_KEY not configured");
            return key;
        }

        public string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = GetJwtKey(); // ✅ Use method to get key
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("token_type", "access"),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
                Issuer = "wonderland-backend",
                Audience = "wonderland-frontend",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var jwtKey = GetJwtKey(); // ✅ Use method to get key
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = "wonderland-frontend",
                ValidateIssuer = true,
                ValidIssuer = "wonderland-backend",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                var jwtSecurityToken = securityToken as JwtSecurityToken;

                if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        public (string accessToken, string refreshToken) GenerateTokens(User user)
        {
            return (GenerateAccessToken(user), GenerateRefreshToken());
        }
    }
}