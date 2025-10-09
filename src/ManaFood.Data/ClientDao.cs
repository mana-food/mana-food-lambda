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
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            
            const string query = @"
                SELECT id, cpf, name, email, user_type, created_at, deleted 
                FROM users 
                WHERE cpf = @cpf AND deleted = 0";

            await using var command = new MySqlCommand(query, connection);
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
                UserType = reader.IsDBNull(reader.GetOrdinal("user_type")) ? 1 : reader.GetInt32("user_type"),
                CreatedAt = reader.GetDateTime("created_at"),
                Deleted = reader.GetBoolean("deleted")
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
        }
    }
}