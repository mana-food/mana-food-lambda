using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ManaFood.Domain;
using Microsoft.IdentityModel.Tokens;

namespace ManaFood.Data;

public class JwtGenerator
{
    private readonly string _secretKey;
    private readonly int _expirationMinutes;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtGenerator(string secretKey, string issuer, string audience, int expirationMinutes = 1440)
    {
        _secretKey = secretKey;
        _expirationMinutes = expirationMinutes;
        _issuer = issuer;
        _audience = audience;
    }

    public (string Token, int ExpiresIn) GenerateToken(Client client)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var role = client.UserType switch
        {
            0 => "ADMIN",
            1 => "CUSTOMER", 
            2 => "KITCHEN",
            3 => "OPERATOR",
            4 => "MANAGER",
            _ => "CUSTOMER"
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, client.Id.ToString()),
            new Claim(ClaimTypes.Name, client.Name ?? client.Cpf),
            new Claim(ClaimTypes.Email, client.Email ?? string.Empty),
            new Claim("role", role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expirationTime = DateTime.UtcNow.AddMinutes(_expirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expirationTime,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var expiresIn = (int)(expirationTime - DateTime.UtcNow).TotalSeconds;

        return (tokenString, expiresIn);
    }
}