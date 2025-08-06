using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using HackerNewsApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HackerNewsApi.Infrastructure.Services;

public class HackerNewsService : IHackerNewsService
{
    private readonly IHackerNewsApiClient _apiClient;
    private readonly ICacheService _cacheService;
    private readonly SearchDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HackerNewsService> _logger;

    private static readonly TimeSpan StoryIdsCacheExpiry = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StoryCacheExpiry = TimeSpan.FromMinutes(90);

    public HackerNewsService(
        IHackerNewsApiClient apiClient,
        ICacheService cacheService,
        SearchDbContext dbContext,
        IServiceProvider serviceProvider,
        ILogger<HackerNewsService> logger)
    {
        _apiClient = apiClient;
        _cacheService = cacheService;
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<int>> GetNewStoryIdsAsync()
    {
        const string cacheKey = "story_ids";

        var cachedIds = await _cacheService.GetAsync<int[]>(cacheKey);
        if (cachedIds != null)
        {
            _logger.LogDebug("Retrieved {Count} story IDs from cache", cachedIds.Length);
            return cachedIds;
        }

        _logger.LogInformation("Cache miss for story IDs, fetching from API");
        var storyIds = await _apiClient.GetNewStoryIdsAsync();
        var storyIdsArray = storyIds.ToArray();

        await _cacheService.SetAsync(cacheKey, storyIdsArray, StoryIdsCacheExpiry);
        _logger.LogInformation("Cached {Count} story IDs", storyIdsArray.Length);

        return storyIdsArray;
    }

    public async Task<Story?> GetStoryAsync(int id)
    {
        var cacheKey = $"story_{id}";

        // Check cache first
        var cachedStory = await _cacheService.GetAsync<Story>(cacheKey);
        if (cachedStory != null)
        {
            _logger.LogDebug("Retrieved story {StoryId} from cache", id);
            return cachedStory;
        }

        // Check database second
        var dbStory = await _dbContext.Stories.FindAsync(id);
        if (dbStory != null)
        {
            _logger.LogDebug("Retrieved story {StoryId} from database", id);
            var story = MapToStory(dbStory);
            
            // Cache the story from database for future requests
            await _cacheService.SetAsync(cacheKey, story, StoryCacheExpiry);
            return story;
        }

        // Finally, fetch from API
        _logger.LogDebug("Story {StoryId} not found in cache or database, fetching from API", id);
        var apiStory = await _apiClient.GetStoryAsync(id);

        if (apiStory != null)
        {
            await _cacheService.SetAsync(cacheKey, apiStory, StoryCacheExpiry);
            _logger.LogDebug("Cached story {StoryId} from API", id);
        }

        return apiStory;
    }

    public async Task<PagedResult<Story>> GetStoriesAsync(int page = 1, int pageSize = 20)
    {
        return await GetStoriesFromApi(page, pageSize);
    }

    private async Task<PagedResult<Story>> GetStoriesFromApi(int page, int pageSize)
    {
        _logger.LogInformation("Getting stories page {Page} with size {PageSize} from API", page, pageSize);

        var storyIds = await GetNewStoryIdsAsync();

        var pagedIds = storyIds
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        var stories = new List<Story>();
        foreach (var id in pagedIds)
        {
            var story = await GetStoryAsync(id);
            if (story != null)
            {
                stories.Add(story);
            }
        }

        var result = new PagedResult<Story>
        {
            Items = stories,
            Page = page,
            PageSize = pageSize
        };

        return result;
    }

    private static Story MapToStory(StoryEntity entity)
    {
        // Convert DateTime back to Unix timestamp for Time property
        var unixTime = ((DateTimeOffset)entity.CreatedAt).ToUnixTimeSeconds();
        
        return new Story
        {
            Id = entity.Id,
            Title = entity.Title,
            By = entity.Author,
            Url = entity.Url,
            Score = entity.Score,
            Time = unixTime,
            Descendants = entity.CommentCount,
            Type = "story"
        };
    }
}