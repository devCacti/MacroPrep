using MacroPrep.Server.Data.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MacroPrep.Server.Services
{
    // THE INTERFACE
    public interface ITokenService
    {
        string GenerateToken(UserEntity user, UserSession session);
    }

    // THE IMPLEMENTATION
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        public TokenService(IConfiguration config) => _config = config;

        public string GenerateToken(UserEntity user, UserSession session)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Subject Claim (User ID)
                new Claim(ClaimTypes.Name, user.UserName), // Unique Name Claim (Username)
                new Claim("sid", session.Id.ToString()), // Session ID Claim
                new Claim("setup_completed", user.HasCompletedSetup.ToString().ToLower())
            };

            var tokenExpiration = DateTime.UtcNow.AddMinutes(15);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: tokenExpiration,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}