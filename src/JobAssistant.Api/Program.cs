using System.Text.Json.Serialization;
using JobAssistant.Api.Data;
using JobAssistant.Api.Endpoints;
using JobAssistant.Api.Ingestion;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("JobAssistant")));

builder.Services.AddSingleton(TimeProvider.System);

// Typed HttpClients for each job board's public API.
builder.Services.AddHttpClient<GreenhouseClient>(client =>
{
    client.BaseAddress = new Uri("https://boards-api.greenhouse.io/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("JobApplicationAssistant/0.1");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<LeverClient>(client =>
{
    client.BaseAddress = new Uri("https://api.lever.co/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("JobApplicationAssistant/0.1");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// The ingestion service receives every board client and picks one per company.
builder.Services.AddTransient<IJobBoardClient>(sp => sp.GetRequiredService<GreenhouseClient>());
builder.Services.AddTransient<IJobBoardClient>(sp => sp.GetRequiredService<LeverClient>());
builder.Services.AddScoped<IngestionService>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

var app = builder.Build();

await DatabaseInitializer.EnsureCreatedAsync(app.Services, app.Logger);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Job Assistant API v1"));
}

app.MapJobEndpoints();
app.MapCompanyEndpoints();
app.MapIngestionEndpoints();

app.Run();

// Lets integration tests reference the entry point (WebApplicationFactory<Program>).
public partial class Program { }
