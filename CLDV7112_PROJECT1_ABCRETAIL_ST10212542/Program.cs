
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
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Only send cookie over HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict;            // Prevent cross-site cookie leaking
});

// Configure HSTS (HTTP Strict Transport Security) to force HTTPS for 1 year
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = false; // Set to true only if you intend to submit to the HSTS preload list
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
    app.UseHsts(); // Sends Strict-Transport-Security header to browsers
}

app.UseHttpsRedirection(); // Redirect all HTTP requests to HTTPS

// Add security headers to every response to prevent browser warnings
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");       // Prevent MIME sniffing
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");           // Prevent clickjacking
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");      // Enable XSS filter
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
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
