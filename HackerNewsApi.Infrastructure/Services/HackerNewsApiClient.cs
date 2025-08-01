using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HackerNewsApi.Infrastructure.Services;

public class HackerNewsApiClient : IHackerNewsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HackerNewsApiClient> _logger;
    private const string BaseUrl = "https://hacker-news.firebaseio.com/v0/";

    public HackerNewsApiClient(HttpClient httpClient, ILogger<HackerNewsApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<IEnumerable<int>> GetNewStoryIdsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching new story IDs from HackerNews API");
            
            var response = await _httpClient.GetAsync("newstories.json");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var storyIds = JsonSerializer.Deserialize<int[]>(json) ?? Array.Empty<int>();
            
            _logger.LogInformation("Fetched {Count} story IDs", storyIds.Length);
            return storyIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching story IDs from HackerNews API");
            throw;
        }
    }

    public async Task<Story?> GetStoryAsync(int id)
    {
        try
        {
            _logger.LogDebug("Fetching story {StoryId} from HackerNews API", id);
            
            var response = await _httpClient.GetAsync($"item/{id}.json");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Story {StoryId} not found or API error", id);
                return null;
            }
            
            var json = await response.Content.ReadAsStringAsync();
            var story = JsonSerializer.Deserialize<Story>(json);
            
            if (story == null || story.Type != "story")
            {
                _logger.LogDebug("Item {StoryId} is not a story or is null", id);
                return null;
            }
            
            return story;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching story {StoryId} from HackerNews API", id);
            throw;
        }
    }
}