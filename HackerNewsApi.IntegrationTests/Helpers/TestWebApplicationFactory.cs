using HackerNewsApi.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HackerNewsApi.IntegrationTests.Helpers;

public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SearchDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add a test database
            services.AddDbContext<SearchDbContext>(options =>
            {
                options.UseSqlite($"Data Source=test_integration_{Guid.NewGuid()}.db");
            });

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