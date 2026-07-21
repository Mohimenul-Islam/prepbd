using PrepBD.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<QuestionService>();
builder.Services.AddHttpClient<EvaluationService>();

// Allowed client origins. Defaults to the local dev servers; in hosted
// environments set ALLOWED_ORIGINS to a comma-separated list of client URLs.
var allowedOrigins = (builder.Configuration["ALLOWED_ORIGINS"]
        ?? "http://localhost:5173,http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowReactDev");
app.MapControllers();

// Root ping so a browser hitting the deployed API URL gets a clear signal.
app.MapGet("/", () => Results.Ok(new { name = "PrepBD API", status = "running" }));

app.Run();
