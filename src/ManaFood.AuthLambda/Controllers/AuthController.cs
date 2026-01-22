using Microsoft.AspNetCore.Mvc;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using ManaFood.AuthLambda.Services;
using ManaFood.AuthLambda.Models;

namespace ManaFood.AuthLambda.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly JwtGenerator _jwtGenerator;

    public AuthController()
    {
        var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
        var serviceUrl = Environment.GetEnvironmentVariable("AWS_SERVICE_URL");
        
        var config = new AmazonDynamoDBConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };
        if (!string.IsNullOrEmpty(serviceUrl))
            config.ServiceURL = serviceUrl;

        _dynamoDbClient = new AmazonDynamoDBClient(config);

        var jwtSecret = Environment.GetEnvironmentVariable("Jwt__SecretKey") 
            ?? throw new InvalidOperationException("Jwt__SecretKey not configured");
        var jwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer") ?? "ManaFood";
        var jwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience") ?? "ManaFoodUsers";
        var expirationMinutes = int.Parse(Environment.GetEnvironmentVariable("Jwt__ExpirationMinutes") ?? "60");

        _jwtGenerator = new JwtGenerator(jwtSecret, jwtIssuer, jwtAudience, expirationMinutes);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.Cpf) || string.IsNullOrWhiteSpace(request?.Password))
            {
                return BadRequest(new { message = "CPF and Password are required" });
            }

            var usersTable = Table.LoadTable(_dynamoDbClient, "mana-food-users");

            var search = usersTable.Query(new QueryOperationConfig
            {
                IndexName = "CpfIndex",
                KeyExpression = new Expression
                {
                    ExpressionStatement = "Cpf = :cpf",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":cpf", request.Cpf }
                    }
                }
            });

            var documents = await search.GetNextSetAsync();
            var userDoc = documents.FirstOrDefault();

            if (userDoc == null)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var storedPassword = userDoc["Password"]?.AsString();
            var hashedPassword = HashPassword(request.Password);

            if (storedPassword != hashedPassword)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var user = new Services.UserInfo
            {
                Id = userDoc["Id"]?.AsString() ?? string.Empty,
                Name = userDoc["Name"]?.AsString() ?? string.Empty,
                Email = userDoc["Email"]?.AsString() ?? string.Empty,
                Cpf = userDoc["Cpf"]?.AsString() ?? string.Empty,
                UserType = (int)(userDoc["UserType"]?.AsInt() ?? 0)
            };

            var (token, expiresIn) = _jwtGenerator.GenerateToken(user);

            return Ok(new
            {
                token,
                expiresIn,
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.UserType
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}