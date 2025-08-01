using HackerNewsApi.Core.Models;

namespace HackerNewsApi.Core.Interfaces;

public interface IHackerNewsService
{
    /// Get the list of newest story IDs from HackerNews
    Task<IEnumerable<int>> GetNewStoryIdsAsync();

    Task<Story?> GetStoryAsync(int id);

    /// Get paginated list of newest stories
    Task<PagedResult<Story>> GetStoriesAsync(int page = 1, int pageSize = 20);

    /// Search stories by title containing the query string
    Task<PagedResult<Story>> SearchStoriesAsync(string query, int page = 1, int pageSize = 20);
}