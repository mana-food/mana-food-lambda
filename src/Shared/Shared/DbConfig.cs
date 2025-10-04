namespace Shared;

public sealed class DbConfig
{
    public string ConnectionString { get; }

    public DbConfig(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required", nameof(connectionString));

        ConnectionString = connectionString;
    }

    public static DbConfig FromEnv(string envVar = "CONNECTION_STRING")
        => new(Environment.GetEnvironmentVariable(envVar));
}
