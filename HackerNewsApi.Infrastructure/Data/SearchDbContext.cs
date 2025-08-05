using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HackerNewsApi.Infrastructure.Data;

public class SearchDbContext : DbContext
{
    private readonly ILogger<SearchDbContext>? _logger;

    public SearchDbContext(DbContextOptions<SearchDbContext> options) : base(options)
    {
    }

    public SearchDbContext(DbContextOptions<SearchDbContext> options, ILogger<SearchDbContext> logger) : base(options)
    {
        _logger = logger;
    }

    public DbSet<StoryEntity> Stories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // HackerNews provides ID

            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Author).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Url).HasMaxLength(1000);
            entity.Property(e => e.Domain).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

            // Indexes for performance
            entity.HasIndex(e => e.Score);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Author);
            entity.HasIndex(e => e.Domain);
            entity.HasIndex(e => e.IndexedAt);
            entity.HasIndex(e => new { e.CreatedAt, e.Score });

            // Computed properties are not mapped
            entity.Ignore(e => e.HasUrl);
            entity.Ignore(e => e.HackerNewsUrl);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite();
        }
    }

    public async Task<int> GetTotalStoriesCountAsync()
    {
        return await Stories.CountAsync();
    }

    public async Task<DateTime?> GetLastIndexedDateAsync()
    {
        return await Stories
            .Where(s => s.IndexedAt.HasValue)
            .MaxAsync(s => (DateTime?)s.IndexedAt);
    }

    public async Task<DateTime?> GetOldestStoryDateAsync()
    {
        return await Stories
            .MinAsync(s => (DateTime?)s.CreatedAt);
    }

    public async Task<DateTime?> GetNewestStoryDateAsync()
    {
        return await Stories
            .MaxAsync(s => (DateTime?)s.CreatedAt);
    }

    public async Task<IEnumerable<string>> GetPopularDomainsAsync(int limit = 20)
    {
        return await Stories
            .Where(s => !string.IsNullOrEmpty(s.Domain))
            .GroupBy(s => s.Domain)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.Key!)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetPopularAuthorsAsync(int limit = 20)
    {
        return await Stories
            .GroupBy(s => s.Author)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.Key)
            .ToListAsync();
    }

    public async Task RebuildFtsIndexAsync()
    {
        try
        {
            // Clear existing FTS data
            await Database.ExecuteSqlRawAsync("DELETE FROM StoriesSearch;");
            
            // Rebuild FTS index from current data with enhanced structure
            // All databases should have the enhanced structure via migrations
            await Database.ExecuteSqlRawAsync(@"
                INSERT INTO StoriesSearch(rowid, Title, Author, Domain, Score, CreatedAt, CommentCount, HasUrl)
                SELECT Id, Title, Author, COALESCE(Domain, ''), Score, CreatedAt, CommentCount,
                       CASE WHEN Url IS NOT NULL AND Url != '' THEN 1 ELSE 0 END
                FROM Stories;
            ");
            
            _logger?.LogDebug("Successfully rebuilt FTS index with enhanced structure");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to rebuild FTS index: {Error}", ex.Message);
            throw;
        }
    }
}