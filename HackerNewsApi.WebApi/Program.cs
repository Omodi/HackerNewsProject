using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Infrastructure.Services;
using HackerNewsApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000;
    options.CompactionPercentage = 0.25;
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddDbContext<SearchDbContext>(options =>
    options.UseSqlite("Data Source=search.db"));

builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IHackerNewsService, HackerNewsService>();
builder.Services.AddScoped<ISearchRepository, SearchRepository>();

// Register DatabaseMaintenanceService as singleton for health monitoring
builder.Services.AddSingleton<HackerNewsApi.Infrastructure.Services.DatabaseMaintenanceService>();
builder.Services.AddHostedService<HackerNewsApi.Infrastructure.Services.StoryIndexingService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<HackerNewsApi.Infrastructure.Services.DatabaseMaintenanceService>());

builder.Services.AddHttpClient<IHackerNewsApiClient, HackerNewsApiClient>()
    .AddPolicyHandler(GetRetryPolicy());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                "http://localhost:4201",
                "https://localhost:4201")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

public partial class Program { }
