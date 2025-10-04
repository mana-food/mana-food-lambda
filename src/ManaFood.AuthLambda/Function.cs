using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
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
        var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING") 
            ?? throw new InvalidOperationException("MYSQL_CONNECTION_STRING not configured");
        
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
            ?? throw new InvalidOperationException("JWT_SECRET not configured");
        
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "ManaFoodIssuer";
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "ManaFoodAudience";

        _clientDao = new ClientDao(connectionString);
        _jwtGenerator = new JwtGenerator(jwtSecret, jwtIssuer, jwtAudience);
    }

    public static async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(
        APIGatewayHttpApiV2ProxyRequest request, 
        ILambdaContext context)
    {
        context.Logger.LogInformation($"Request: {request.RequestContext.Http.Method} {request.RawPath}");

        if (!string.Equals(request.RequestContext.Http.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return CreateResponse(HttpStatusCode.MethodNotAllowed, new { error = "Only POST method is allowed" });
        }

        AuthRequest? authRequest = null;
        if (!string.IsNullOrEmpty(request.Body))
        {
            try
            {
                authRequest = JsonSerializer.Deserialize<AuthRequest>(request.Body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                context.Logger.LogError($"JSON parsing error: {ex.Message}");
                return CreateResponse(HttpStatusCode.BadRequest, new { error = "Invalid JSON format" });
            }
        }

        if (string.IsNullOrWhiteSpace(authRequest?.Cpf))
        {
            return CreateResponse(HttpStatusCode.BadRequest, new { error = "CPF is required" });
        }

        try
        {
            var client = await _clientDao.GetByCpfAsync(authRequest.Cpf);
            
            if (client == null)
            {
                context.Logger.LogWarning($"Client not found for CPF: {authRequest.Cpf}");
                return CreateResponse(HttpStatusCode.Unauthorized, new { error = "Client not found" });
            }

            var (token, expiresIn) = _jwtGenerator.GenerateToken(client);

            context.Logger.LogInformation($"Token generated successfully for CPF: {authRequest.Cpf}");

            return CreateResponse(HttpStatusCode.OK, new
            {
                token,
                expires_in = expiresIn,
                client = new
                {
                    cpf = client.Cpf,
                    name = client.Name,
                    email = client.Email
                }
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing request: {ex.Message}");
            return CreateResponse(HttpStatusCode.InternalServerError, new { error = "Internal server error" });
        }
    }

    private static APIGatewayHttpApiV2ProxyResponse CreateResponse(HttpStatusCode statusCode, object body)
    {
        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = (int)statusCode,
            Body = JsonSerializer.Serialize(body),
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Access-Control-Allow-Origin", "*" },
                { "Access-Control-Allow-Headers", "Content-Type" },
                { "Access-Control-Allow-Methods", "POST, OPTIONS" }
            }
        };
    }

    public static async Task Main()
    {
        Func<APIGatewayHttpApiV2ProxyRequest, ILambdaContext, Task<APIGatewayHttpApiV2ProxyResponse>> handler = FunctionHandler;
        await LambdaBootstrapBuilder
            .Create(handler, new DefaultLambdaJsonSerializer())
            .Build()
            .RunAsync();
    }
}