using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using ManaFood.AuthLambda.Models;
using ManaFood.AuthLambda.Services;
using ManaFood.Data;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace ManaFood.AuthLambda;

public class Function
{
    private static readonly SecretsManagerService _secretsService;
    private static readonly JwtGenerator _jwtGenerator;
    private static ClientDao? _clientDao;

    static Function()
    {
        _secretsService = new SecretsManagerService();
        
        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        var appSettingsJson = File.ReadAllText(appSettingsPath);
        var appSettings = JsonSerializer.Deserialize<AppSettings>(appSettingsJson);

        var jwtSecret = appSettings?.Jwt?.SecretKey
            ?? throw new InvalidOperationException("Jwt:SecretKey not configured");

        var jwtIssuer = appSettings?.Jwt?.Issuer ?? "ManaFoodIssuer";
        var jwtAudience = appSettings?.Jwt?.Audience ?? "ManaFoodAudience";
        var expirationMinutes = appSettings?.Jwt?.ExpirationMinutes ?? 1440;

        _jwtGenerator = new JwtGenerator(jwtSecret, jwtIssuer, jwtAudience, expirationMinutes);
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"API Gateway request: {request.Path} {request.HttpMethod}");
            context.Logger.LogInformation($"Body: {request.Body}");

            AuthRequest? authRequest = null;
            if (!string.IsNullOrWhiteSpace(request.Body))
            {
                try
                {
                    authRequest = JsonSerializer.Deserialize<AuthRequest>(request.Body);
                }
                catch (JsonException ex)
                {
                    context.Logger.LogError($"Invalid JSON: {ex.Message}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = 400,
                        Body = JsonSerializer.Serialize(new { message = "Invalid JSON format" }),
                        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                    };
                }
            }

            if (authRequest?.Cpf == null || string.IsNullOrWhiteSpace(authRequest.Cpf))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new { message = "CPF is required" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            if (_clientDao == null)
            {
                var secretArn = Environment.GetEnvironmentVariable("AURORA_SECRET_ARN")
                    ?? throw new InvalidOperationException("AURORA_SECRET_ARN not configured");
                
                context.Logger.LogInformation("Getting database credentials from Secrets Manager...");
                var credentials = await _secretsService.GetDatabaseCredentialsAsync(secretArn);
                var connectionString = credentials.ToConnectionString();
                
                _clientDao = new ClientDao(connectionString);
                context.Logger.LogInformation("Database connection initialized");
            }

            context.Logger.LogInformation($"Searching for CPF: {authRequest.Cpf}");

            var client = await _clientDao.GetByCpfAsync(authRequest.Cpf);

            if (client == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new { message = "Client not found" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            var (token, expiresIn) = _jwtGenerator.GenerateToken(client);

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new
                {
                    token,
                    expiresIn
                }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error: {ex}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(new { message = "Internal server error", error = ex.Message }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }

    private static async Task Main()
    {
        await LambdaBootstrapBuilder.Create<APIGatewayProxyRequest, APIGatewayProxyResponse>(
            new Function().FunctionHandler, 
            new DefaultLambdaJsonSerializer())
            .Build()
            .RunAsync();
    }
}

public class AppSettings
{
    public JwtSettings? Jwt { get; set; }
}

public class JwtSettings
{
    public string? SecretKey { get; set; }
    public int ExpirationMinutes { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
}