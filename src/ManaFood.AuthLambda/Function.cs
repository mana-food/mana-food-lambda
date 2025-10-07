using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using ManaFood.AuthLambda.Models;
using ManaFood.Data;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace ManaFood.AuthLambda;

public class Function
{
    private static readonly ClientDao _clientDao;
    private static readonly JwtGenerator _jwtGenerator;

    static Function()
    {
        // Ler configurações direto do arquivo JSON
        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        var appSettingsJson = File.ReadAllText(appSettingsPath);
        var appSettings = JsonSerializer.Deserialize<AppSettings>(appSettingsJson);

        // Connection string vem de variável de ambiente
        var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING")
            ?? throw new InvalidOperationException("MYSQL_CONNECTION_STRING not configured");

        // JWT configs vêm do appsettings.json
        var jwtSecret = appSettings?.Jwt?.SecretKey
            ?? throw new InvalidOperationException("Jwt:SecretKey not configured");

        var jwtIssuer = appSettings?.Jwt?.Issuer ?? "ManaFoodIssuer";
        var jwtAudience = appSettings?.Jwt?.Audience ?? "ManaFoodAudience";
        var expirationMinutes = appSettings?.Jwt?.ExpirationMinutes ?? 1440;

        _clientDao = new ClientDao(connectionString);
        _jwtGenerator = new JwtGenerator(jwtSecret, jwtIssuer, jwtAudience, expirationMinutes);
    }

    public async Task<object> FunctionHandler(AuthRequest? request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"Request received: {JsonSerializer.Serialize(request)}");

            if (request?.Cpf == null || string.IsNullOrWhiteSpace(request.Cpf))
            {
                return new
                {
                    statusCode = 400,
                    body = JsonSerializer.Serialize(new { message = "CPF is required" })
                };
            }

            context.Logger.LogInformation($"Searching for CPF: {request.Cpf}");

            var client = await _clientDao.GetByCpfAsync(request.Cpf);

            if (client == null)
            {
                return new
                {
                    statusCode = 404,
                    body = JsonSerializer.Serialize(new { message = "Client not found" })
                };
            }

            var (token, expiresIn) = _jwtGenerator.GenerateToken(client);

            return new
            {
                statusCode = 200,
                body = JsonSerializer.Serialize(new
                {
                    token,
                    expiresIn
                })
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error: {ex}");
            return new
            {
                statusCode = 500,
                body = JsonSerializer.Serialize(new { message = "Internal server error", error = ex.Message })
            };
        }
    }

    private static async Task Main()
    {
        await LambdaBootstrapBuilder.Create<AuthRequest, object>(
            new Function().FunctionHandler, 
            new DefaultLambdaJsonSerializer())
            .Build()
            .RunAsync();
    }
}

// Classes para deserializar o appsettings.json
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