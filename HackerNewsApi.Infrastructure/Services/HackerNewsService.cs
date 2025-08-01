using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using Microsoft.Extensions.Logging;

namespace HackerNewsApi.Infrastructure.Services;

public class HackerNewsService : IHackerNewsService
{
    private readonly IHackerNewsApiClient _apiClient;
    private readonly ICacheService _cacheService;
    private readonly ILogger<HackerNewsService> _logger;

    private static readonly TimeSpan StoryIdsCacheExpiry = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StoryCacheExpiry = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SearchCacheExpiry = TimeSpan.FromMinutes(2);

    public HackerNewsService(
        IHackerNewsApiClient apiClient,
        ICacheService cacheService,
        ILogger<HackerNewsService> logger)
    {
        _apiClient = apiClient;
        _cacheService = cacheService;
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
        
        var cachedStory = await _cacheService.GetAsync<Story>(cacheKey);
        if (cachedStory != null)
        {
            _logger.LogDebug("Retrieved story {StoryId} from cache", id);
            return cachedStory;
        }

        _logger.LogDebug("Cache miss for story {StoryId}, fetching from API", id);
        var story = await _apiClient.GetStoryAsync(id);
        
        if (story != null)
        {
            await _cacheService.SetAsync(cacheKey, story, StoryCacheExpiry);
            _logger.LogDebug("Cached story {StoryId}", id);
        }
        
        return story;
    }

    public async Task<PagedResult<Story>> GetStoriesAsync(int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("Getting stories page {Page} with size {PageSize}", page, pageSize);
        
        var storyIds = await GetNewStoryIdsAsync();
        var totalCount = storyIds.Count();
        
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

        return new PagedResult<Story>
        {
            Items = stories,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Story>> SearchStoriesAsync(string query, int page = 1, int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetStoriesAsync(page, pageSize);
        }

        var cacheKey = $"search_{query.ToLowerInvariant()}_{page}_{pageSize}";
        var cachedResult = await _cacheService.GetAsync<PagedResult<Story>>(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Retrieved search results from cache for query: {Query}", query);
            return cachedResult;
        }

        _logger.LogInformation("Searching stories for query: {Query}, page {Page}", query, page);
        
        var storyIds = await GetNewStoryIdsAsync();
        var allStories = new List<Story>();
        
        foreach (var id in storyIds.Take(500)) // Limit search to first 500 stories for performance
        {
            var story = await GetStoryAsync(id);
            if (story != null && story.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                allStories.Add(story);
            }
        }

        var totalCount = allStories.Count;
        var pagedStories = allStories
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PagedResult<Story>
        {
            Items = pagedStories,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        await _cacheService.SetAsync(cacheKey, result, SearchCacheExpiry);
        _logger.LogInformation("Found {Count} stories matching query: {Query}", totalCount, query);
        
        return result;
    }
}