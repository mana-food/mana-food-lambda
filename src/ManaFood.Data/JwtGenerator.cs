using ManaFood.Domain;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ManaFood.Data;

public class JwtGenerator
{
    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtGenerator(string secretKey, string issuer, string audience, int expirationMinutes = 30)
    {
        // Garantir que a chave tenha no mínimo 32 bytes (256 bits)
        if (secretKey.Length < 32)
        {
            secretKey = secretKey.PadRight(32, '0');
        }
        
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        _issuer = issuer;
        _audience = audience;
        _expirationMinutes = expirationMinutes;
    }

    public (string Token, int ExpiresIn) GenerateToken(Client client)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_expirationMinutes);

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
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var expiresIn = (int)(expires - now).TotalSeconds;

        return (tokenString, expiresIn);
    }
}