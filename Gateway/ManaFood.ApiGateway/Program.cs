using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

// HttpClient para falar com a Lambda
builder.Services.AddHttpClient("auth");

// Rate limit (global, por IP)
builder.Services.AddRateLimiter(o =>
{
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                AutoReplenishment = true
            });
    });
});

// CORS (dev simplificado; ajuste quando tiver front definido)
builder.Services.AddCors(o => 
{
    o.AddDefaultPolicy(p => 
    {
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod();
    });
});

// YARP (carrega rotas/clusters do appsettings)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// JWT – usa Authority/JWKS se houver; senão, chave simétrica (SigningKeyB64)
var cfg = builder.Configuration;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        var authority = cfg["Auth:Authority"];
        if (!string.IsNullOrWhiteSpace(authority))
        {
            // Fluxo com provedor de identidade (JWKS)
            opts.Authority = authority;
            opts.Audience  = cfg["Auth:Audience"];
            opts.RequireHttpsMetadata = true;
        }
        else
        {
            // Fluxo “sem provedor”: valida com chave simétrica (apenas DEV/POC)
            var keyB64 = cfg["Auth:SigningKeyB64"]!;
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(keyB64)),
                ValidateIssuer = true, ValidIssuer = cfg["Auth:Issuer"],
                ValidateAudience = true, ValidAudience = cfg["Auth:Audience"],
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                NameClaimType = "sub", RoleClaimType = "role"
            };
        }
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("RequireClientRole", p => p.RequireRole("CLIENT"));
});

var app = builder.Build();

app.UseRateLimiter();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Health
app.MapGet("/", () => "ManaFood Gateway OK").AllowAnonymous();

// /auth/cpf → chama a Lambda e devolve { token, ... } para o cliente
app.MapPost("/auth/cpf", async (HttpContext ctx, IHttpClientFactory http) =>
{
    var req = await ctx.Request.ReadFromJsonAsync<CpfRequest>();
    if (string.IsNullOrWhiteSpace(req?.Cpf))
        return Results.BadRequest(new { error = "cpf_required" });

    var client = http.CreateClient("auth");
    var lambdaUrl = app.Configuration["Auth:LambdaUrl"]; // ex: https://.../auth-cpf
    if (string.IsNullOrWhiteSpace(lambdaUrl))
        return Results.StatusCode(500);

    var resp = await client.PostAsJsonAsync(lambdaUrl, new { cpf = req.Cpf });
    if (!resp.IsSuccessStatusCode)
        return Results.Unauthorized();

    // Espera JSON (combine com a colega): { token, expires_in, ... }
    var payload = await resp.Content.ReadFromJsonAsync<object>();
    return Results.Ok(payload);
})
.AllowAnonymous()
.WithName("AuthenticateByCpf");

// Proxy (políticas vêm do appsettings)
app.MapReverseProxy();

app.Run();

public record CpfRequest(string Cpf);
