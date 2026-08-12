
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services;
using Stripe;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Session – SameSite=Lax required for Stripe payment redirects
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax; // Lax allows Stripe redirect to carry session
});

// HSTS – 1 year, enforces HTTPS for all browsers
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = false;
});

// Configure Stripe API key globally
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Azure Storage connection string
var connectionString = builder.Configuration["AzureStorage:ConnectionString"];
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Azure Storage ConnectionString is missing in appsettings.json.");

// Register all services
builder.Services.AddSingleton(new TableStorageService(connectionString));
builder.Services.AddSingleton(new BlobStorageService(connectionString));
builder.Services.AddSingleton(new QueueStorageService(connectionString));
builder.Services.AddSingleton(new FileShareService(connectionString));
builder.Services.AddSingleton<StripePaymentService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
