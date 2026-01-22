var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();