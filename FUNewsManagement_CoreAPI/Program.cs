using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FUNews.BusinessLogic.Extensions;
using FUNews.BusinessLogic.Options;
using FUNews.DataAccess.Extensions;
using FUNews.DataAccess.Seeding;
using ManhMD_SE1930_A01_BE.Middleware;
using ManhMD_SE1930_A01_BE.OData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.IdentityModel.Tokens;

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
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FUNews Management API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Nhập JWT Bearer token theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configuration & Connection string
var connectionString = builder.Configuration.GetConnectionString("FUNewsManagement")
    ?? throw new InvalidOperationException("Connection string 'FUNewsManagement' not found in configuration.");

// Support environment variable FUNEWS_JWT_SIGNING_KEY or User Secrets override
var envSigningKey = Environment.GetEnvironmentVariable("FUNEWS_JWT_SIGNING_KEY");
if (!string.IsNullOrWhiteSpace(envSigningKey))
{
    builder.Configuration["Jwt:SigningKey"] = envSigningKey;
}

// Register DataAccess (Scoped DbContext, Scoped DAOs & Repositories, Sequences, PasswordHasher)
builder.Services.AddFUNewsDataAccess(connectionString);

// Register BusinessLogic (Singleton Options/Config, Scoped Application Services)
builder.Services.AddFUNewsBusinessLogic(builder.Configuration);

// Configure JWT Authentication & Authorization
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt section is missing from configuration.");

// In production environment, do NOT allow default/demo insecure secret keys
if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) ||
        jwtOptions.SigningKey.Contains("PRN232_Secret_Key", StringComparison.OrdinalIgnoreCase) ||
        jwtOptions.SigningKey.Contains("Default_Secret", StringComparison.OrdinalIgnoreCase) ||
        Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
    {
        throw new InvalidOperationException("FATAL: Insecure default JWT signing key detected in Production environment! Please configure a secure key (at least 32 bytes) via environment variable 'FUNEWS_JWT_SIGNING_KEY' or production secrets.");
    }
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Staff"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Staff", "Admin"));
    options.AddPolicy("LecturerOnly", policy => policy.RequireRole("Lecturer"));
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make Program accessible for WebApplicationFactory in integration tests
public partial class Program { }

