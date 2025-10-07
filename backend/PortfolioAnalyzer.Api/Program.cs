var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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
