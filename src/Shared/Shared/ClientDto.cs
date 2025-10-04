namespace Shared;

public sealed class ClientDto
{
    public Guid   Id    { get; init; }
    public string Name  { get; init; } = "";
    public string Cpf   { get; init; } = "";
    public string Email { get; init; } = "";
}
