using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using HackerNewsApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HackerNewsApi.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IMemoryCache memoryCache, IServiceScopeFactory serviceScopeFactory, ILogger<CacheService> logger)
    {
        _memoryCache = memoryCache;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        _memoryCache.TryGetValue(key, out T? cachedValue);
        return Task.FromResult(cachedValue);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiry)
    {
        var options = CreateCacheEntryOptions(key, value, expiry);
        
        _memoryCache.Set(key, value, options);

        _ = Task.Run(async () => await PersistToDatabase(value));

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    private MemoryCacheEntryOptions CreateCacheEntryOptions<T>(string key, T value, TimeSpan expiry)
    {
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = expiry,
            Size = CalculateCacheSize(key, value),
            Priority = DetermineCachePriority(key)
        };

        // Add eviction callback for monitoring
        options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
        {
            var keyString = evictedKey?.ToString() ?? "unknown";
            _logger.LogDebug("Cache entry evicted: Key={Key}, Reason={Reason}",
                keyString, reason);
        });

        return options;
    }

    private int CalculateCacheSize<T>(string key, T value)
    {
        return value switch
        {
            Story when key.StartsWith("story_") => 1,
            
            Array array => 5,
            
            _ => 1
        };
    }

    private CacheItemPriority DetermineCachePriority(string key)
    {
        return key switch
        {
            "story_ids" => CacheItemPriority.High,
            
            var k when k.StartsWith("story_") => CacheItemPriority.Normal,
            
            _ => CacheItemPriority.Low
        };
    }

    private async Task PersistToDatabase<T>(T value)
    {
        try
        {
            if (value is Story story)
            {
                // Create a new scope to get a fresh DbContext for the background operation
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
                await PersistStoryToDatabase(story, dbContext);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Background database persistence failed. Cache operation was successful.");
        }
    }

    private async Task PersistStoryToDatabase(Story story, SearchDbContext dbContext)
    {
        var existingStory = await dbContext.Stories.FindAsync(story.Id);

        var storyEntity = new StoryEntity
        {
            Id = story.Id,
            Title = story.Title,
            Author = story.By,
            Url = story.Url,
            Score = story.Score,
            CreatedAt = story.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            CommentCount = story.CommentCount,
            Domain = ExtractDomain(story.Url),
            IndexedAt = DateTime.UtcNow
        };

        if (existingStory == null)
        {
            dbContext.Stories.Add(storyEntity);
            _logger.LogDebug("Background: Adding cached story to database: {StoryId}", story.Id);
        }
        else
        {
            dbContext.Entry(existingStory).CurrentValues.SetValues(storyEntity);
            _logger.LogDebug("Background: Updating cached story in database: {StoryId}", story.Id);
        }

        await dbContext.SaveChangesAsync();
        _logger.LogDebug("Background: Successfully persisted cached story {StoryId} to database", story.Id);
    }


    private static string? ExtractDomain(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        try
        {
            var uri = new Uri(url);
            return uri.Host.Replace("www.", "");
        }
        catch
        {
            return null;
        }
    }
}