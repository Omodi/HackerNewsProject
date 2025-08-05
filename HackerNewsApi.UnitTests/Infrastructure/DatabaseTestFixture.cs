using HackerNewsApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HackerNewsApi.UnitTests.Infrastructure;

/// <summary>
/// Centralized test fixture for database setup and cleanup
/// </summary>
public class DatabaseTestFixture : IDisposable
{
    private readonly string _databasePath;
    private readonly DbContextOptions<SearchDbContext> _contextOptions;
    private bool _disposed = false;

    public DatabaseTestFixture()
    {
        _databasePath = $"test_{Guid.NewGuid()}.db";
        _contextOptions = new DbContextOptionsBuilder<SearchDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .EnableSensitiveDataLogging()
            .Options;

        using var context = CreateContext();
        
        context.Database.EnsureDeleted();
        
        context.Database.Migrate();
    }

    public SearchDbContext CreateContext()
    {
        return new SearchDbContext(_contextOptions);
    }

    public SearchDbContext CreateContextWithLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<SearchDbContext>();
        return new SearchDbContext(_contextOptions, logger);
    }

    public DbContextOptions<SearchDbContext> ContextOptions => _contextOptions;

    public void Dispose()
    {
        if (!_disposed)
        {
            // Clean up the test database file
            if (File.Exists(_databasePath))
            {
                try
                {
                    // Ensure all connections are closed
                    using (var context = CreateContext())
                    {
                        context.Database.CloseConnection();
                    }
                    
                    // Force garbage collection to release any remaining handles
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    File.Delete(_databasePath);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Base class for tests that need a database
/// </summary>
public abstract class DatabaseTestBase : IClassFixture<DatabaseTestFixture>, IDisposable
{
    protected readonly DatabaseTestFixture DatabaseFixture;
    private SearchDbContext? _context;

    protected DatabaseTestBase(DatabaseTestFixture databaseFixture)
    {
        DatabaseFixture = databaseFixture;
    }

    protected SearchDbContext Context
    {
        get
        {
            _context ??= DatabaseFixture.CreateContext();
            return _context;
        }
    }

    protected SearchDbContext CreateNewContext()
    {
        return DatabaseFixture.CreateContext();
    }

    /// <summary>
    /// Clean all data from the database to ensure test isolation
    /// </summary>
    protected async Task CleanDatabaseAsync()
    {
        try
        {
            using var context = CreateNewContext();
            // Delete all data from tables
            await context.Database.ExecuteSqlRawAsync("DELETE FROM Stories");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM StoriesSearch");
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Clean database before disposing context
            CleanDatabaseAsync().GetAwaiter().GetResult();
            _context?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}