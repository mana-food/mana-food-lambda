using Amazon.DynamoDBv2.DocumentModel;

namespace ManaFood.AuthLambda.Services;

public interface IUserService
{
    Task<Document?> GetUserByCpfAsync(string cpf);
}