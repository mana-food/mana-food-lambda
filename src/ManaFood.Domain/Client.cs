namespace ManaFood.Domain;

public class Client
{
    public Guid Id { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserType { get; set; } 
    public bool Deleted { get; set; }
}