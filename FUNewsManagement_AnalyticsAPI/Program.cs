using System.Text;
using FUNewsManagement_AnalyticsAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers & HttpContextAccessor
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
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

// Core Data Client
var coreApiUrl = builder.Configuration["ApiSettings:CoreApiUrl"] ?? "https://localhost:7001";
builder.Services.AddHttpClient<ICoreDataClient, CoreDataClient>(client =>
{
    client.BaseAddress = new Uri(coreApiUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Analytics & Excel Services
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Swagger Configuration with JWT Bearer
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FUNews Analytics API",
        Version = "v1",
        Description = "Microservice cung cấp thống kê Dashboard, danh sách Trending, gợi ý bài viết liên quan và xuất Excel."
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FUNews Analytics API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
