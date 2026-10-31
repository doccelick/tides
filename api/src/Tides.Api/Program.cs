using Tides.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiInfrastructure(builder.Configuration);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddTideServices();
builder.Services.AddAdminAuth(builder.Configuration);

var app = builder.Build();

app.UseApiMiddleware();
app.MapApiEndpoints();

app.Run();
