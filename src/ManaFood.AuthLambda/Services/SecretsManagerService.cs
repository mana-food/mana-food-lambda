using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace ManaFood.AuthLambda.Services;

public class SecretsManagerService
{
    private readonly IAmazonSecretsManager _secretsManager;
    
    public SecretsManagerService()
    {
        _secretsManager = new AmazonSecretsManagerClient();
    }
    
    public async Task<DatabaseCredentials> GetDatabaseCredentialsAsync(string secretArn)
    {
        try
        {
            var request = new GetSecretValueRequest
            {
                SecretId = secretArn
            };
            
            var response = await _secretsManager.GetSecretValueAsync(request);
            var secretData = JsonSerializer.Deserialize<DatabaseSecret>(response.SecretString);
            
            return new DatabaseCredentials
            {
                Host = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? throw new InvalidOperationException("DATABASE_HOST not configured"),
                Port = int.Parse(Environment.GetEnvironmentVariable("DATABASE_PORT") ?? "3306"),
                Database = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "manafooddb",
                Username = secretData.Username,
                Password = secretData.Password
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get database credentials: {ex.Message}", ex);
        }
    }
}

public class DatabaseSecret
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class DatabaseCredentials
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    
    public string ToConnectionString()
    {
        return $"Server={Host};Port={Port};Database={Database};Uid={Username};Pwd={Password};SslMode=Required;";
    }
}