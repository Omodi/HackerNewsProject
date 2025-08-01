using HackerNewsApi.Core.Models;

namespace HackerNewsApi.Core.Interfaces;

public interface IHackerNewsApiClient
{
    Task<IEnumerable<int>> GetNewStoryIdsAsync();
    Task<Story?> GetStoryAsync(int id);
}