using System.Text.Json;
using System.Text.Json.Serialization;
using FUNews.BusinessLogic.Extensions;
using FUNews.DataAccess.Extensions;
using FUNews.DataAccess.Seeding;
using ManhMD_SE1930_A01_BE.Middleware;
using ManhMD_SE1930_A01_BE.OData;
using Microsoft.AspNetCore.OData;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container with JSON camelCase and OData configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .AddOData(options =>
    {
        options.Select().Filter().OrderBy().Count().SetMaxTop(100);
        options.AddRouteComponents("api", ODataModelBuilder.GetEdmModel());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuration & Connection string
var connectionString = builder.Configuration.GetConnectionString("FUNewsManagement")
    ?? throw new InvalidOperationException("Connection string 'FUNewsManagement' not found in configuration.");

// Register DataAccess (Scoped DbContext, Scoped DAOs & Repositories, Sequences, PasswordHasher)
builder.Services.AddFUNewsDataAccess(connectionString);

// Register BusinessLogic (Singleton Options/Config, Scoped Application Services)
builder.Services.AddFUNewsBusinessLogic(builder.Configuration);

var app = builder.Build();

// Global Exception Handling Middleware (RFC 7807 ProblemDetails)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Ensure EDM model is attached to all API requests for OData query parsing
var edmModel = ODataModelBuilder.GetEdmModel();
app.Use((context, next) =>
{
    var odataFeature = Microsoft.AspNetCore.OData.Extensions.HttpRequestExtensions.ODataFeature(context.Request);
    if (odataFeature.Model == null)
    {
        odataFeature.Model = edmModel;
    }
    return next();
});

// Run legacy password seeder to ensure unhashed seed passwords are cryptographically hashed
try
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IPasswordSeeder>();
    await seeder.SeedLegacyPasswordsAsync();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Failed to run legacy password seeder during application startup.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Make Program accessible for WebApplicationFactory in integration tests
public partial class Program { }

