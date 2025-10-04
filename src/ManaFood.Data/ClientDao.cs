using ManaFood.Domain;
using MySqlConnector;

namespace ManaFood.Data;

public class ClientDao
{
    private readonly string _connectionString;

    public ClientDao(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Client?> GetByCpfAsync(string cpf)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(
            "SELECT id, cpf, name, email, created_at, deleted, user_type FROM users WHERE cpf = @cpf AND deleted = 0",
            connection);
        
        command.Parameters.AddWithValue("@cpf", cpf);

        await using var reader = await command.ExecuteReaderAsync();
        
        if (!await reader.ReadAsync())
            return null;

        return new Client
        {
            Id = reader.GetGuid("id"),
            Cpf = reader.GetString("cpf"),
            Name = reader.IsDBNull(reader.GetOrdinal("name")) ? null : reader.GetString("name"),
            Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
            CreatedAt = reader.GetDateTime("created_at"),
            Deleted = reader.GetBoolean("deleted"),
            UserType = reader.GetInt32("user_type")
        };
    }
}