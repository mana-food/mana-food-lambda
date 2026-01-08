using Amazon.DynamoDBv2;
using ManaFood.AuthLambda.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    var config = new AmazonDynamoDBConfig 
    { 
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(
            Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1"
        )
    };
    
    var serviceUrl = Environment.GetEnvironmentVariable("AWS_SERVICE_URL");
    if (!string.IsNullOrEmpty(serviceUrl))
        config.ServiceURL = serviceUrl;

    return new AmazonDynamoDBClient(config);
});

builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

await app.RunAsync();