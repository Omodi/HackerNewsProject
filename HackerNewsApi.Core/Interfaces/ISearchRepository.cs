using HackerNewsApi.Core.Models;

namespace HackerNewsApi.Core.Interfaces;

public interface ISearchRepository
{
    /// <summary>
    /// Performs a full-text search across indexed stories
    /// </summary>
    Task<PagedResult<Story>> SearchStoriesAsync(SearchQuery query);
    
    /// <summary>
    /// Provides search suggestions based on partial query
    /// </summary>
    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string partialQuery, int limit = 10);
    
    /// <summary>
    /// Indexes a single story for search
    /// </summary>
    Task IndexStoryAsync(Story story);
    
    /// <summary>
    /// Indexes multiple stories in batch
    /// </summary>
    Task IndexStoriesAsync(IEnumerable<Story> stories);
    
    /// <summary>
    /// Checks if a story is already indexed
    /// </summary>
    Task<bool> IsStoryIndexedAsync(int storyId);
    
    /// <summary>
    /// Rebuilds the entire search index (admin operation)
    /// </summary>
    Task RebuildIndexAsync();
}