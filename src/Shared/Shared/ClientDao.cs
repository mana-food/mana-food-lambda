using MySqlConnector;

namespace Shared;

public sealed class ClientDao
{
    private readonly DbConfig _cfg;

    public ClientDao(DbConfig cfg) => _cfg = cfg;

    public async Task<ClientDto?> GetByCpfAsync(string cpf, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return null;

        await using var conn = new MySqlConnection(_cfg.ConnectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT Id, Name, Cpf, Email
            FROM Users
            WHERE Deleted = 0 AND REPLACE(Cpf, '.', '') = REPLACE(@cpf, '.', '')
            LIMIT 1;";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@cpf", cpf);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new ClientDto
        {
            Id    = reader.GetGuid("Id"),
            Name  = reader.GetString("Name"),
            Cpf   = reader.GetString("Cpf"),
            Email = reader.GetString("Email")
        };
    }
}
