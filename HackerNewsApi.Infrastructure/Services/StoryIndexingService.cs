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
    private readonly TimeSpan _indexingInterval = TimeSpan.FromMinutes(15);
    
    private const int INITIAL_BULK_SIZE = 5000;
    private const int BULK_PAGE_SIZE = 500;
    private const int INCREMENTAL_CHECK_SIZE = 20;

    public StoryIndexingService(IServiceProvider serviceProvider, ILogger<StoryIndexingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _indexingSemaphore = new SemaphoreSlim(1, 1); // Only allow one indexing operation at a time
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Story indexing service started");

        // Check if database is empty and needs initial seeding
        if (await IsDatabaseEmptyAsync())
        {
            _logger.LogInformation("Database is empty - performing initial bulk seeding");
            await PerformInitialBulkSeeding(stoppingToken);
        }

        // Start periodic incremental updates
        using var timer = new PeriodicTimer(_indexingInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PerformIncrementalUpdate(stoppingToken);
        }
    }

    private async Task<bool> IsDatabaseEmptyAsync()
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
            _logger.LogWarning(ex, "Could not check database state, assuming empty database");
            return false;
        }
    }

    private async Task PerformInitialBulkSeeding(CancellationToken cancellationToken)
    {
        if (!await _indexingSemaphore.WaitAsync(1000, cancellationToken))
        {
            _logger.LogWarning("Skipping initial seeding - another indexing operation is in progress");
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var hackerNewsService = scope.ServiceProvider.GetRequiredService<IHackerNewsService>();
            var searchRepository = scope.ServiceProvider.GetRequiredService<ISearchRepository>();

            _logger.LogInformation("Starting initial bulk seeding with {BulkSize} stories using page size {PageSize}",
                INITIAL_BULK_SIZE, BULK_PAGE_SIZE);

            var allStories = new List<Story>();
            var totalPages = (int)Math.Ceiling((double)INITIAL_BULK_SIZE / BULK_PAGE_SIZE);
            
            // Fetch stories in chunks to respect the 1000 story API limit
            for (int page = 1; page <= totalPages && !cancellationToken.IsCancellationRequested; page++)
            {
                try
                {
                    _logger.LogDebug("Fetching page {Page} of {TotalPages} ({PageSize} stories per page)",
                        page, totalPages, BULK_PAGE_SIZE);
                    
                    var storiesResult = await hackerNewsService.GetStoriesAsync(page, BULK_PAGE_SIZE);
                    
                    if (storiesResult?.Items?.Any() == true)
                    {
                        var validStories = storiesResult.Items.Where(s => !string.IsNullOrEmpty(s.Title)).ToList();
                        allStories.AddRange(validStories);
                        
                        _logger.LogDebug("Page {Page}: Retrieved {Count} valid stories (total so far: {Total})",
                            page, validStories.Count, allStories.Count);
                    }
                    else
                    {
                        _logger.LogWarning("Page {Page}: No stories retrieved", page);
                        break;
                    }
                    
                    // Add small delay between requests to be respectful to the API
                    if (page < totalPages)
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching page {Page} during bulk seeding", page);
                }
            }

            if (allStories.Count > 0)
            {
                _logger.LogInformation("Retrieved {Count} total stories across {Pages} pages for bulk seeding",
                    allStories.Count, totalPages);

                await searchRepository.IndexStoriesAsync(allStories);
                
                _logger.LogInformation("Initial bulk seeding completed: {Count} stories indexed", allStories.Count);
            }
            else
            {
                _logger.LogWarning("No stories retrieved for initial bulk seeding");
            }
        }
        finally
        {
            _indexingSemaphore.Release();
        }
    }

    private async Task PerformIncrementalUpdate(CancellationToken cancellationToken)
    {
        if (!await _indexingSemaphore.WaitAsync(1000, cancellationToken))
        {
            _logger.LogWarning("Skipping incremental update - another indexing operation is in progress");
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var hackerNewsService = scope.ServiceProvider.GetRequiredService<IHackerNewsService>();
            var searchRepository = scope.ServiceProvider.GetRequiredService<ISearchRepository>();

            _logger.LogDebug("Starting incremental update - checking {CheckSize} most recent stories", INCREMENTAL_CHECK_SIZE);

            // Get the most recent stories directly using GetStoriesAsync
            var recentStoriesResult = await hackerNewsService.GetStoriesAsync(1, INCREMENTAL_CHECK_SIZE);
            
            if (recentStoriesResult?.Items?.Any() != true)
            {
                _logger.LogWarning("No recent stories retrieved for incremental update");
                return;
            }

            var recentStories = recentStoriesResult.Items.Where(s => !string.IsNullOrEmpty(s.Title)).ToList();

            // Check which ones are not already indexed
            var newStories = new List<Story>();
            foreach (var story in recentStories)
            {
                if (!await searchRepository.IsStoryIndexedAsync(story.Id))
                {
                    newStories.Add(story);
                }
            }

            if (newStories.Count == 0)
            {
                _logger.LogDebug("All {CheckSize} recent stories are already indexed - no new content", INCREMENTAL_CHECK_SIZE);
                return;
            }

            _logger.LogInformation("Found {NewCount} new stories out of {CheckedCount} recent stories",
                newStories.Count, recentStories.Count);

            // Index the new stories
            await searchRepository.IndexStoriesAsync(newStories);
            _logger.LogInformation("Incremental update completed: indexed {Count} new stories", newStories.Count);
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