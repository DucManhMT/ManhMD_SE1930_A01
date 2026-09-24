using FUNews.BusinessLogic.Extensions;
using FUNews.DataAccess.Extensions;
using FUNews.DataAccess.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuration & Connection string
var connectionString = builder.Configuration.GetConnectionString("FUNewsManagement")
    ?? throw new InvalidOperationException("Connection string 'FUNewsManagement' not found in configuration.");

// Register DataAccess (Scoped DbContext, Scoped DAOs & Repositories, Sequences, PasswordHasher)
builder.Services.AddFUNewsDataAccess(connectionString);

// Register BusinessLogic (Singleton Options/Config)
builder.Services.AddFUNewsBusinessLogic(builder.Configuration);

var app = builder.Build();

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
