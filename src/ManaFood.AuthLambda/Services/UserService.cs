using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace ManaFood.AuthLambda.Services;

public class UserService : IUserService
{
    private readonly IAmazonDynamoDB _dynamoDb;

    public UserService(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
    }

    public async Task<Document?> GetUserByCpfAsync(string cpf)
    {
        var table = Table.LoadTable(_dynamoDb, "mana-food-users");
        var search = table.Query(new QueryOperationConfig
        {
            IndexName = "CpfIndex",
            KeyExpression = new Expression
            {
                ExpressionStatement = "Cpf = :cpf",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":cpf", cpf }
                }
            }
        });

        var documents = await search.GetNextSetAsync();
        return documents.FirstOrDefault();
    }
}