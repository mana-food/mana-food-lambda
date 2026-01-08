using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.IdentityModel.Tokens;

namespace ManaFood.AuthLambda.Services;

public class AuthService
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _jwtSecret;
    private readonly int _expirationMinutes;

    public AuthService(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        _jwtSecret = Environment.GetEnvironmentVariable("Jwt__SecretKey") 
            ?? throw new InvalidOperationException("Jwt__SecretKey not configured");
        _expirationMinutes = int.Parse(Environment.GetEnvironmentVariable("Jwt__ExpirationMinutes") ?? "60");
    }

    public async Task<object?> AuthenticateAsync(string cpf, string password)
    {
        var table = Table.LoadTable(_dynamoDb, "mana-food-users");
        var search = table.Query(new QueryOperationConfig
        {
            IndexName = "CpfIndex",
            KeyExpression = new Expression
            {
                ExpressionStatement = "Cpf = :cpf",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":cpf", cpf }
                }
            }
        });

        var documents = await search.GetNextSetAsync();
        var userDoc = documents.FirstOrDefault();

        if (userDoc == null)
            return null;

        // Verificar senha
        var storedPassword = userDoc["Password"]?.AsString();
        if (storedPassword != HashPassword(password))
            return null;

        // Gerar token
        var userId = userDoc["Id"]?.AsString() ?? string.Empty;
        var userName = userDoc["Name"]?.AsString() ?? string.Empty;
        var userEmail = userDoc["Email"]?.AsString() ?? string.Empty;
        var userType = userDoc["UserType"]?.AsInt() ?? 1;

        var token = GenerateJwtToken(userId, userName, userEmail, cpf, userType);

        return new
        {
            token,
            expiresIn = _expirationMinutes * 60,
            user = new
            {
                id = userId,
                name = userName,
                email = userEmail,
                userType
            }
        };
    }

    private string GenerateJwtToken(string id, string name, string email, string cpf, int userType)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", id),
            new Claim("name", name),
            new Claim("email", email),
            new Claim("cpf", cpf),
            new Claim("role", GetRoleName(userType)),
            new Claim("jti", Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: Environment.GetEnvironmentVariable("Jwt__Issuer") ?? "ManaFood",
            audience: Environment.GetEnvironmentVariable("Jwt__Audience") ?? "ManaFoodUsers",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static string GetRoleName(int userType) => userType switch
    {
        0 => "ADMIN",
        1 => "CUSTOMER",
        2 => "KITCHEN",
        3 => "OPERATOR",
        4 => "MANAGER",
        _ => "CUSTOMER"
    };
}