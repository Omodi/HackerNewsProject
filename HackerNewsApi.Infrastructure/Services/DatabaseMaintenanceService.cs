using HackerNewsApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HackerNewsApi.Infrastructure.Services;

public class DatabaseMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMaintenanceService> _logger;
    
    // Configuration for Azure 1GB limit
    private const long MaxDatabaseSizeBytes = 800_000_000; // 800MB limit (leave 200MB buffer)
    private const int DefaultRetentionDays = 30;
    private const int CleanupIntervalHours = 6;
    
    public DatabaseMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMaintenanceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database maintenance service started (Max size: {MaxSize}MB)", 
            MaxDatabaseSizeBytes / 1_000_000);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceAsync();
                await Task.Delay(TimeSpan.FromHours(CleanupIntervalHours), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database maintenance");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }

    private async Task PerformMaintenanceAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SearchDbContext>();

        var stats = await GetDatabaseStatsAsync(dbContext);
        
        _logger.LogInformation("Database stats: Size={SizeMB}MB, Stories={StoryCount}, " +
                             "Oldest={OldestDate}, Usage={UsagePercent:F1}%",
            stats.SizeInMB, stats.StoryCount, stats.OldestStoryDate?.ToString("yyyy-MM-dd"), 
            stats.UsagePercentage);

        // Clean up if approaching size limit or old data exists
        if (stats.SizeInBytes > MaxDatabaseSizeBytes || stats.ShouldCleanupOldData)
        {
            await CleanupOldDataAsync(dbContext, stats);
        }

        if (stats.UsagePercentage > 70)
        {
            await OptimizeDatabaseAsync(dbContext);
        }
    }

    private async Task<DatabaseStats> GetDatabaseStatsAsync(SearchDbContext dbContext)
    {
        var dbPath = GetDatabasePath(dbContext);
        var sizeInBytes = 0L;
        
        if (File.Exists(dbPath))
        {
            var fileInfo = new FileInfo(dbPath);
            sizeInBytes = fileInfo.Length;
        }

        var storyCount = await dbContext.Stories.CountAsync();
        var oldestStory = await dbContext.Stories
            .OrderBy(s => s.IndexedAt)
            .Select(s => new { s.IndexedAt })
            .FirstOrDefaultAsync();
        
        var newestStory = await dbContext.Stories
            .OrderByDescending(s => s.IndexedAt)
            .Select(s => new { s.IndexedAt })
            .FirstOrDefaultAsync();

        var cutoffDate = DateTime.UtcNow.AddDays(-DefaultRetentionDays);
        var oldDataExists = oldestStory?.IndexedAt < cutoffDate;

        return new DatabaseStats
        {
            SizeInBytes = sizeInBytes,
            SizeInMB = sizeInBytes / 1_000_000.0,
            UsagePercentage = (double)sizeInBytes / MaxDatabaseSizeBytes * 100,
            StoryCount = storyCount,
            OldestStoryDate = oldestStory?.IndexedAt,
            NewestStoryDate = newestStory?.IndexedAt,
            ShouldCleanupOldData = oldDataExists,
            MaxSizeMB = MaxDatabaseSizeBytes
        };
    }

    private async Task CleanupOldDataAsync(SearchDbContext dbContext, DatabaseStats stats)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-DefaultRetentionDays);
        
        _logger.LogInformation("Starting cleanup: removing stories older than {CutoffDate}", 
            cutoffDate.ToString("yyyy-MM-dd"));

        var batchSize = 1000;
        var deletedCount = 0;

        while (true)
        {
            var oldStories = await dbContext.Stories
                .Where(s => s.IndexedAt < cutoffDate)
                .Take(batchSize)
                .ToListAsync();

            if (!oldStories.Any())
                break;

            dbContext.Stories.RemoveRange(oldStories);
            await dbContext.SaveChangesAsync();
            
            deletedCount += oldStories.Count;
            _logger.LogDebug("Deleted batch of {Count} old stories (total: {Total})", 
                oldStories.Count, deletedCount);

            // If database is still too large, increase retention aggressiveness
            var currentStats = await GetDatabaseStatsAsync(dbContext);
            if (currentStats.SizeInBytes > MaxDatabaseSizeBytes)
            {
                cutoffDate = DateTime.UtcNow.AddDays(-7);
                _logger.LogWarning("Database still over limit, reducing retention to 7 days");
            }
        }

        if (deletedCount > 0)
        {
            _logger.LogInformation("Cleanup completed: deleted {Count} old stories", deletedCount);
        }
    }

    private async Task OptimizeDatabaseAsync(SearchDbContext dbContext)
    {
        _logger.LogInformation("Optimizing database to reclaim space");
        
        try
        {
            // SQLite VACUUM command to rebuild database and reclaim space
            await dbContext.Database.ExecuteSqlRawAsync("VACUUM;");
            
            _logger.LogInformation("Database optimization completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database optimization failed");
        }
    }

    private string GetDatabasePath(SearchDbContext dbContext)
    {
        var connectionString = dbContext.Database.GetConnectionString();
        
        if (connectionString?.Contains("Data Source=") == true)
        {
            var dataSourceIndex = connectionString.IndexOf("Data Source=") + "Data Source=".Length;
            var semicolonIndex = connectionString.IndexOf(';', dataSourceIndex);
            
            if (semicolonIndex > 0)
                return connectionString.Substring(dataSourceIndex, semicolonIndex - dataSourceIndex);
            else
                return connectionString.Substring(dataSourceIndex);
        }
        
        return "search.db"; // Default fallback
    }

    public async Task<DatabaseStats> GetCurrentStatsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        return await GetDatabaseStatsAsync(dbContext);
    }

    public async Task ForceCleanupAsync()
    {
        _logger.LogInformation("Manual database cleanup requested");
        await PerformMaintenanceAsync();
    }
}

public class DatabaseStats
{
    public long SizeInBytes { get; set; }
    public double SizeInMB { get; set; }
    public double UsagePercentage { get; set; }
    public int StoryCount { get; set; }
    public DateTime? OldestStoryDate { get; set; }
    public DateTime? NewestStoryDate { get; set; }
    public bool ShouldCleanupOldData { get; set; }
    public long MaxSizeMB { get; set; }
    public bool IsNearLimit => UsagePercentage > 80;
    public bool IsOverLimit => UsagePercentage > 100;
}