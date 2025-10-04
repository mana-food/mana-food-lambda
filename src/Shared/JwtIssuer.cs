using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Shared;

public static class JwtIssuer
{
    public static string IssueToken(
        string signingKeyB64,
        string issuer,
        string audience,
        ClientDto client,
        TimeSpan? ttl = null)
    {
        var keyBytes = Convert.FromBase64String(signingKeyB64);
        var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, client.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, client.Email),
            new Claim("cpf", client.Cpf),
            new Claim(ClaimTypes.Role, "CLIENT")
        };

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.Add(ttl ?? TimeSpan.FromMinutes(30)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
