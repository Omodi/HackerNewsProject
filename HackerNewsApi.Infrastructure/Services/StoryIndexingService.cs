using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using HackerNewsApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HackerNewsApi.Infrastructure.Services;

public class StoryIndexingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StoryIndexingService> _logger;
    private readonly SemaphoreSlim _indexingSemaphore;

    public StoryIndexingService(IServiceProvider serviceProvider, ILogger<StoryIndexingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _indexingSemaphore = new SemaphoreSlim(1, 1); // Only allow one indexing operation at a time
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Story indexing service started");

        // Check if we have stories, if not perform bulk indexing
        if (await ShouldPerformBulkIndexing())
        {
            await PerformBulkStoryIndexing(stoppingToken);
        }
        else
        {
            _logger.LogInformation("Stories already exist in database, skipping bulk indexing");
        }
        
        _logger.LogInformation("Story indexing service completed");
    }

    private async Task<bool> ShouldPerformBulkIndexing()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
            
            // Ensure database is created and migrations are applied
            await context.Database.MigrateAsync();
            
            var totalStories = await context.GetTotalStoriesCountAsync();
            
            _logger.LogInformation("Database currently has {TotalStories} stories indexed", totalStories);
            return totalStories == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check database state, assuming we should perform bulk indexing");
            return true;
        }
    }

    private async Task PerformBulkStoryIndexing(CancellationToken cancellationToken)
    {
        if (!await _indexingSemaphore.WaitAsync(1000, cancellationToken))
        {
            _logger.LogWarning("Skipping bulk indexing - another indexing operation is in progress");
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var hackerNewsService = scope.ServiceProvider.GetRequiredService<IHackerNewsService>();
            var searchRepository = scope.ServiceProvider.GetRequiredService<ISearchRepository>();

            _logger.LogInformation("Starting bulk story indexing - fetching story IDs from HackerNews API");

            var storyIds = await hackerNewsService.GetNewStoryIdsAsync();
            var storyIdsList = storyIds.ToList();
            
            _logger.LogInformation("Retrieved {Count} story IDs from the API", storyIdsList.Count);

            var stories = new List<Story>();

            // Fetch each story by ID
            foreach (var storyId in storyIdsList)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var story = await hackerNewsService.GetStoryAsync(storyId);
                    if (story != null && !string.IsNullOrEmpty(story.Title))
                    {
                        stories.Add(story);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error fetching story {StoryId}", storyId);
                }
            }

            _logger.LogInformation("Fetched {ValidCount} valid stories", stories.Count);

            if (stories.Count > 0)
            {
                await searchRepository.IndexStoriesAsync(stories);
                _logger.LogInformation("Bulk story indexing completed: {Count} stories indexed", stories.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk story indexing");
        }
        finally
        {
            _indexingSemaphore.Release();
        }
    }

    public override void Dispose()
    {
        _indexingSemaphore?.Dispose();
        base.Dispose();
    }
}