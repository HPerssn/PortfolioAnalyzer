using AspNetCoreRateLimit;
using Microsoft.EntityFrameworkCore;
using PortfolioAnalyzer.Core.Data;

var builder = WebApplication.CreateBuilder(args);

// Add database context
builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<PortfolioRepository>();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry();

// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Add CORS with specific allowed origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                            ?? new[] { "http://localhost:5173" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

// Use IP rate limiting
app.UseIpRateLimiting();

// Serve static files (frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

// API routes
app.MapControllers();

// Fallback to index.html for SPA routing (Vue Router)
// Only catch routes that don't start with /api
app.MapFallback(context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = 404;
        return Task.CompletedTask;
    }
    context.Response.Redirect("/index.html");
    return Task.CompletedTask;
});

app.Run();
