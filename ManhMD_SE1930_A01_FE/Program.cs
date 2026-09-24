using FUNews.Client.BusinessLogic.Extensions;
using FUNews.Client.DataAccess.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Register typed HTTP client and client business logic services
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7001";
builder.Services.AddFUNewsClientDataAccess(apiBaseUrl);
builder.Services.AddFUNewsClientBusinessLogic();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
