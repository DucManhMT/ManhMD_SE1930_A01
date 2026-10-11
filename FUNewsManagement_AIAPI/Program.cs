using System.Text;
using FUNewsManagement_AIAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers & Memory Cache
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();

// JWT Authentication
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FUNewsApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FUNewsWeb";
var jwtKey = builder.Configuration["Jwt:SigningKey"] ?? "FUNewsManagement_PRN232_Secret_Key_For_Jwt_Auth_2026_Secure!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// HttpClient calling Core API for Tag Catalog
var coreApiUrl = builder.Configuration["ApiSettings:CoreApiUrl"] ?? "https://localhost:7001";
builder.Services.AddHttpClient<ITagCatalogClient, TagCatalogClient>(client =>
{
    client.BaseAddress = new Uri(coreApiUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// AI Services
builder.Services.AddSingleton<ILearningCacheService, LearningCacheService>();
builder.Services.AddScoped<ITagExtractorService, TagExtractorService>();

// Swagger Configuration with JWT Bearer
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FUNews AI API",
        Version = "v1",
        Description = "Microservice gợi ý thẻ tin thông minh (Tags) và duy trì bộ nhớ học (Learning Cache)."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header sử dụng scheme Bearer. Ví dụ: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FUNews AI API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
