using HackerNewsApi.Core.Models;

namespace HackerNewsApi.Core.Interfaces;

public interface IHackerNewsService
{
    /// <summary>
    /// Get the list of newest story IDs from HackerNews
    /// </summary>
    Task<IEnumerable<int>> GetNewStoryIdsAsync();

    Task<Story?> GetStoryAsync(int id);
    /// <summary>
    /// Get paginated list of newest stories
    /// </summary>
    Task<PagedResult<Story>> GetStoriesAsync(int page = 1, int pageSize = 20);
}