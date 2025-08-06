using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using HackerNewsApi.Infrastructure.Data;
using HackerNewsApi.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace HackerNewsApi.IntegrationTests.Helpers;

public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove background services that make HTTP calls and database operations
            var hostedServices = services.Where(s => s.ServiceType == typeof(IHostedService)).ToList();
            foreach (var service in hostedServices)
            {
                services.Remove(service);
            }

            // Remove specific services registered as hosted services but keep their singleton registrations
            var storyIndexingService = services.SingleOrDefault(d => d.ImplementationType == typeof(StoryIndexingService));
            if (storyIndexingService != null)
                services.Remove(storyIndexingService);

            // Keep DatabaseMaintenanceService as singleton (needed by HealthController) but remove as hosted service
            // The hosted service registration is already removed above in the IHostedService loop

            // Remove ALL DbContext-related registrations to ensure clean test isolation
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<SearchDbContext>) ||
                d.ServiceType == typeof(SearchDbContext) ||
                d.ImplementationType == typeof(SearchDbContext)).ToList();
            
            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add a test database with unique name per test run - this will be used by ALL scopes
            var testConnectionString = $"Data Source=test_integration_{Guid.NewGuid()}.db";
            services.AddDbContext<SearchDbContext>(options =>
            {
                options.UseSqlite(testConnectionString);
            });

            // Replace the HackerNews API client with a mock that provides predictable test data
            var hackerNewsApiClientDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHackerNewsApiClient));
            if (hackerNewsApiClientDescriptor != null)
                services.Remove(hackerNewsApiClientDescriptor);

            // Create mock with predictable test data
            var mockHackerNewsApiClient = new Mock<IHackerNewsApiClient>();
            
            // Mock story IDs - these will be used by the API
            var testStoryIds = new[] { 1001, 1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010,
                                     1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 1019, 1020 };
            
            mockHackerNewsApiClient.Setup(x => x.GetNewStoryIdsAsync())
                .ReturnsAsync(testStoryIds);

            // Mock individual stories - create predictable test stories
            for (int i = 0; i < testStoryIds.Length; i++)
            {
                var storyId = testStoryIds[i];
                var testStory = new Story
                {
                    Id = storyId,
                    Title = $"Test Story {storyId}",
                    By = $"author{i + 1}",
                    Url = $"https://example.com/story{storyId}",
                    Score = 100 + i,
                    Time = DateTimeOffset.UtcNow.AddHours(-i).ToUnixTimeSeconds(), // Use Unix timestamp for Time property
                    Descendants = 10 + i, // This will be returned by CommentCount computed property
                    Type = "story"
                };
                
                mockHackerNewsApiClient.Setup(x => x.GetStoryAsync(storyId))
                    .ReturnsAsync(testStory);
            }

            // Mock non-existent story (like ID 999999999 used in tests)
            mockHackerNewsApiClient.Setup(x => x.GetStoryAsync(999999999))
                .ReturnsAsync((Story?)null);

            services.AddSingleton(mockHackerNewsApiClient.Object);

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<SearchDbContext>();

            // Ensure the database is created and migrated
            db.Database.EnsureDeleted();
            db.Database.Migrate();
        });

        // Set environment to Testing to skip auto-migration in Program.cs
        builder.UseEnvironment("Testing");
    }
}