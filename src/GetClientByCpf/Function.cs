using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Shared;

public class Function
{
    private static ClientDao _dao = null!;
    private static string _issuer = "manafood-auth";
    private static string _audience = "manafood-api";
    private static string _signingKeyB64 = "";

    static Function()
    {
        AWSSDKHandler.RegisterXRayForAllServices();

        var cfg = DbConfig.FromEnv("CONNECTION_STRING");
        _dao = new ClientDao(cfg);

        _issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? _issuer;
        _audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? _audience;
        _signingKeyB64 = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY_B64") ?? "";
    }

    private static async Task Main()
    {
        Func<APIGatewayHttpApiV2ProxyRequest, ILambdaContext, Task<APIGatewayHttpApiV2ProxyResponse>> handler = Handler;
        await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer())
            .Build()
            .RunAsync();
    }

    public static async Task<APIGatewayHttpApiV2ProxyResponse> Handler(APIGatewayHttpApiV2ProxyRequest req, ILambdaContext ctx)
    {
        try
        {
            if (!string.Equals(req.RequestContext.Http.Method, "POST", StringComparison.OrdinalIgnoreCase))
                return Resp((int)HttpStatusCode.MethodNotAllowed, new { error = "only_post" });

            var body = req.Body ?? "{}";
            var payload = JsonSerializer.Deserialize<CpfRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (payload is null || string.IsNullOrWhiteSpace(payload.Cpf))
                return Resp((int)HttpStatusCode.BadRequest, new { error = "cpf_required" });

            var client = await _dao.GetByCpfAsync(payload.Cpf);
            if (client is null)
                return Resp((int)HttpStatusCode.Unauthorized, new { error = "not_found" });

            if (string.IsNullOrWhiteSpace(_signingKeyB64))
                return Resp((int)HttpStatusCode.InternalServerError, new { error = "missing_signing_key" });

            var token = JwtIssuer.IssueToken(_signingKeyB64, _issuer, _audience, client, TimeSpan.FromMinutes(30));
            return Resp((int)HttpStatusCode.OK, new { token, expires_in = 1800 });
        }
        catch (Exception ex)
        {
            ctx.Logger.LogError($"Lambda error: {ex.Message}\n{ex.StackTrace}");
            return Resp((int)HttpStatusCode.InternalServerError, new { error = "internal_error" });
        }
    }

    private static APIGatewayHttpApiV2ProxyResponse Resp(int code, object obj) =>
        new()
        {
            StatusCode = code,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = JsonSerializer.Serialize(obj)
        };

    private sealed record CpfRequest(string Cpf);
}
