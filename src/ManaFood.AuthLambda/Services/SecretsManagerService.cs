using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                Host = secretData.Host ?? throw new InvalidOperationException("Host not found in secret"),
                Port = secretData.Port > 0 ? secretData.Port : 3306,
                Database = secretData.DbClusterIdentifier ?? "manafooddb",
                Username = secretData.Username ?? throw new InvalidOperationException("Username not found in secret"),
                Password = secretData.Password ?? throw new InvalidOperationException("Password not found in secret")
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
    [JsonPropertyName("username")]
    public string? Username { get; set; }
    
    [JsonPropertyName("password")]
    public string? Password { get; set; }
    
    [JsonPropertyName("host")]
    public string? Host { get; set; }
    
    [JsonPropertyName("port")]
    public int Port { get; set; }
    
    [JsonPropertyName("dbClusterIdentifier")]
    public string? DbClusterIdentifier { get; set; }
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