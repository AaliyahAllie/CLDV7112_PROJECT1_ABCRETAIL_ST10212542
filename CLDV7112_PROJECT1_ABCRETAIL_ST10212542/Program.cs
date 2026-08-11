
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register Session State
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Azure Storage Services
var connectionString = builder.Configuration["AzureStorage:ConnectionString"];
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Azure Storage ConnectionString is missing in appsettings.json.");
}

builder.Services.AddSingleton(new TableStorageService(connectionString));
builder.Services.AddSingleton(new BlobStorageService(connectionString));
builder.Services.AddSingleton(new QueueStorageService(connectionString));
builder.Services.AddSingleton(new FileShareService(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Enable session state before Authorization
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
