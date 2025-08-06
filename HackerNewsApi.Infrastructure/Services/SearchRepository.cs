using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using HackerNewsApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace HackerNewsApi.Infrastructure.Services;

public class SearchRepository : ISearchRepository
{
    private readonly SearchDbContext _context;
    private readonly ILogger<SearchRepository> _logger;

    public SearchRepository(SearchDbContext context, ILogger<SearchRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<Story>> SearchStoriesAsync(SearchQuery query)
    {
        try
        {
            // Validate inputs
            ValidateSearchQuery(query);

            _logger.LogInformation("Executing search query: {Query}", query.Query);

            // Get paginated story IDs from FTS search with filters applied
            var storyIds = await GetFtsMatchingStoryIds(query);
            
            // Get the actual story entities from the IDs
            var stories = await GetStoriesFromIds(storyIds, query);

            return new PagedResult<Story>
            {
                Items = stories,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing search query: {Query}", query?.Query ?? "null");
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string partialQuery, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(partialQuery) || partialQuery.Length < 2)
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            var suggestions = new List<string>();

            // Escape any special FTS5 characters and add prefix wildcard
            var searchTerm = EscapeFtsQuery(partialQuery.Trim());

            // Get suggestions using FTS5 with prefix matching
            FormattableString ftsQuery = $"""
                SELECT DISTINCT Title, Author, Domain, Score
                FROM StoriesSearch
                WHERE StoriesSearch MATCH '{searchTerm}'
                ORDER BY Score DESC, CreatedAt DESC
                LIMIT {limit}
                """;

            var results = await _context.Database
                .SqlQueryRaw<SuggestionResult>(ftsQuery.ToString())
                .ToListAsync();

            // Check which columns contain the partial query and add those as suggestions
            foreach (var result in results)
            {
                if (!string.IsNullOrEmpty(result.Title) &&
                    result.Title.Contains(partialQuery, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add(result.Title);
                }
                
                if (!string.IsNullOrEmpty(result.Author) &&
                    result.Author.Contains(partialQuery, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add(result.Author);
                }
                
                if (!string.IsNullOrEmpty(result.Domain) &&
                    result.Domain.Contains(partialQuery, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add(result.Domain);
                }
            }

            return suggestions.Distinct(StringComparer.OrdinalIgnoreCase).Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting FTS search suggestions for: {Query}", partialQuery);
            return Enumerable.Empty<string>();
        }
    }


    private static string EscapeFtsQuery(string query)
    {
        // Escape FTS5 special characters: " ' \ * AND OR NOT
        return query.Replace("\"", "\"\"")
                   .Replace("'", "''")
                   .Replace("\\", "\\\\");
    }

    // Data transfer object for FTS suggestion results
    private class SuggestionResult
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    // Note: Removed CountResult and RowIdResult classes - now using int directly

    private void ValidateSearchQuery(SearchQuery query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        if (query.Page < 1)
            throw new ArgumentException("Page must be greater than 0", nameof(query));

        if (query.PageSize < 1 || query.PageSize > 100)
            throw new ArgumentException("PageSize must be between 1 and 100", nameof(query));

        if (!string.IsNullOrEmpty(query.Query) && query.Query.Length > 200)
            throw new ArgumentException("Search query is too long", nameof(query));
    }

    private async Task<IEnumerable<int>> GetFtsMatchingStoryIds(SearchQuery query)
    {
        try
        {
            // Build only the search query, no count needed
            var ftsQuery = BuildFtsQueries(query);

            // Get paginated results from FTS table
            var matchingIds = await _context.Database
                .SqlQuery<int>(ftsQuery)
                .ToListAsync();

            _logger.LogDebug("FTS search for '{Query}' returned {Count} matches", query.Query, matchingIds.Count);

            return matchingIds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FTS search failed for query '{Query}', returning empty results", query.Query);
            return Enumerable.Empty<int>();
        }
    }

    private async Task<IEnumerable<Story>> GetStoriesFromIds(IEnumerable<int> storyIds, SearchQuery query)
    {
        if (!storyIds.Any())
            return Enumerable.Empty<Story>();

        var baseQuery = _context.Stories.Where(s => storyIds.Contains(s.Id));

        // Apply sorting to maintain the order from the paginated query
        baseQuery = ApplySorting(baseQuery, query.SortBy);

        var results = await baseQuery.ToListAsync();
        return results.Select(entity => MapToStory(entity));
    }

    private FormattableString BuildFtsQueries(SearchQuery query)
    {
        var skip = (query.Page - 1) * query.PageSize;
        var filterClause = GetFtsFilterClause(query.Filters);
        var orderClause = GetFtsOrderClause(query.SortBy, !string.IsNullOrWhiteSpace(query.Query));

        FormattableString ftsQuery;

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            // Escape the search term for FTS but let Entity Framework handle parameterization
            var escapedQuery = EscapeFtsQuery(query.Query);
            
            _logger.LogDebug("Search query: '{Query}' -> escaped: '{EscapedQuery}'", query.Query, escapedQuery);

            // Text search with filters - manually quote the search term since EF doesn't handle FTS quoting
            if (!string.IsNullOrEmpty(filterClause))
            {
                ftsQuery = $"SELECT rowid FROM StoriesSearch WHERE StoriesSearch MATCH '{escapedQuery}' AND {filterClause} {orderClause} LIMIT {query.PageSize} OFFSET {skip}";
            }
            else
            {
                ftsQuery = $"SELECT rowid FROM StoriesSearch WHERE StoriesSearch MATCH '{escapedQuery}' {orderClause} LIMIT {query.PageSize} OFFSET {skip}";
            }
        }
        else
        {
            // Filter-only search (no text search)
            if (!string.IsNullOrEmpty(filterClause))
            {
                ftsQuery = $"SELECT rowid FROM StoriesSearch WHERE {filterClause} {orderClause} LIMIT {query.PageSize} OFFSET {skip}";
            }
            else
            {
                ftsQuery = $"SELECT rowid FROM StoriesSearch {orderClause} LIMIT {query.PageSize} OFFSET {skip}";
            }
        }

        _logger.LogDebug("Generated FTS Query: {FtsQuery}", ftsQuery.ToString());

        return ftsQuery;
    }

    private string GetFtsFilterClause(SearchFilters? filters)
    {
        if (filters == null) return string.Empty;

        var conditions = new List<string>();

        if (filters.FromDate.HasValue)
        {
            conditions.Add($"CreatedAt >= '{filters.FromDate.Value:yyyy-MM-dd HH:mm:ss}'");
        }

        if (filters.ToDate.HasValue)
        {
            conditions.Add($"CreatedAt <= '{filters.ToDate.Value:yyyy-MM-dd HH:mm:ss}'");
        }

        if (filters.MinScore.HasValue)
        {
            conditions.Add($"Score >= {filters.MinScore.Value}");
        }

        if (filters.MaxScore.HasValue)
        {
            conditions.Add($"Score <= {filters.MaxScore.Value}");
        }

        if (!string.IsNullOrEmpty(filters.Author))
        {
            conditions.Add($"Author = '{filters.Author.Replace("'", "''")}'");
        }

        if (!string.IsNullOrEmpty(filters.Domain))
        {
            conditions.Add($"Domain = '{filters.Domain.Replace("'", "''")}'");
        }

        if (filters.HasUrl.HasValue)
        {
            conditions.Add($"HasUrl = {(filters.HasUrl.Value ? 1 : 0)}");
        }

        return string.Join(" AND ", conditions);
    }

    private string GetFtsOrderClause(SearchSortOrder sortBy, bool hasTextSearch)
    {
        return sortBy switch
        {
            SearchSortOrder.Relevance when hasTextSearch => "ORDER BY rank",
            SearchSortOrder.Relevance => "ORDER BY Score DESC, CreatedAt DESC",
            SearchSortOrder.Score => "ORDER BY Score DESC, CreatedAt DESC",
            SearchSortOrder.Recent => "ORDER BY CreatedAt DESC",
            SearchSortOrder.Oldest => "ORDER BY CreatedAt ASC",
            SearchSortOrder.Comments => "ORDER BY CommentCount DESC, Score DESC",
            _ => "ORDER BY Score DESC, CreatedAt DESC"
        };
    }

    private IQueryable<StoryEntity> ApplySearchFilters(IQueryable<StoryEntity> query, SearchFilters? filters)
    {
        if (filters == null) return query;

        if (filters.FromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= filters.FromDate.Value);

        if (filters.ToDate.HasValue)
            query = query.Where(s => s.CreatedAt <= filters.ToDate.Value);

        if (filters.MinScore.HasValue)
            query = query.Where(s => s.Score >= filters.MinScore.Value);

        if (filters.MaxScore.HasValue)
            query = query.Where(s => s.Score <= filters.MaxScore.Value);

        if (!string.IsNullOrEmpty(filters.Author))
            query = query.Where(s => s.Author == filters.Author);

        if (!string.IsNullOrEmpty(filters.Domain))
            query = query.Where(s => s.Domain == filters.Domain);

        if (filters.HasUrl.HasValue)
        {
            if (filters.HasUrl.Value)
                query = query.Where(s => !string.IsNullOrEmpty(s.Url));
            else
                query = query.Where(s => string.IsNullOrEmpty(s.Url));
        }

        return query;
    }

    private IQueryable<StoryEntity> ApplySorting(IQueryable<StoryEntity> query, SearchSortOrder sortBy)
    {
        return sortBy switch
        {
            SearchSortOrder.Relevance => query.OrderByDescending(s => s.Score).ThenByDescending(s => s.CreatedAt),
            SearchSortOrder.Score => query.OrderByDescending(s => s.Score).ThenByDescending(s => s.CreatedAt),
            SearchSortOrder.Recent => query.OrderByDescending(s => s.CreatedAt),
            SearchSortOrder.Oldest => query.OrderBy(s => s.CreatedAt),
            SearchSortOrder.Comments => query.OrderByDescending(s => s.CommentCount).ThenByDescending(s => s.Score),
            _ => query.OrderByDescending(s => s.Score).ThenByDescending(s => s.CreatedAt)
        };
    }

    public async Task IndexStoryAsync(Story story)
    {
        try
        {
            var existingStory = await _context.Stories.FindAsync(story.Id);

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
                _context.Stories.Add(storyEntity);
                _logger.LogDebug("Adding new story to index: {StoryId}", story.Id);
            }
            else
            {
                _context.Entry(existingStory).CurrentValues.SetValues(storyEntity);
                _logger.LogDebug("Updating existing story in index: {StoryId}", story.Id);
            }

            await _context.SaveChangesAsync();
            _logger.LogDebug("Successfully indexed story {StoryId}: {Title}", story.Id, story.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing story {StoryId}: {Title}", story.Id, story.Title);
            throw;
        }
    }

    public async Task IndexStoriesAsync(IEnumerable<Story> stories)
    {
        try
        {
            var storyList = stories.ToList();
            _logger.LogInformation("Bulk indexing {Count} stories", storyList.Count);

            var storyIds = storyList.Select(s => s.Id).ToList();
            
            // Fetch existing stories for update
            var existingStories = await _context.Stories
                .Where(s => storyIds.Contains(s.Id))
                .ToListAsync();

            var existingIds = existingStories.Select(s => s.Id).ToHashSet();

            // Separate new stories from updates
            var newStories = storyList.Where(s => !existingIds.Contains(s.Id)).ToList();
            var storiesToUpdate = storyList.Where(s => existingIds.Contains(s.Id)).ToList();

            // Add new stories
            if (newStories.Any())
            {
                var newStoryEntities = newStories.Select(story => new StoryEntity
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
                }).ToList();

                _context.Stories.AddRange(newStoryEntities);
                _logger.LogDebug("Adding {Count} new stories to index", newStories.Count);
            }

            // Update existing stories
            if (storiesToUpdate.Any())
            {
                foreach (var story in storiesToUpdate)
                {
                    var existingStory = existingStories.First(e => e.Id == story.Id);
                    
                    // Update the properties
                    existingStory.Title = story.Title;
                    existingStory.Author = story.By;
                    existingStory.Url = story.Url;
                    existingStory.Score = story.Score;
                    existingStory.UpdatedAt = DateTime.UtcNow;
                    existingStory.CommentCount = story.CommentCount;
                    existingStory.Domain = ExtractDomain(story.Url);
                    existingStory.IndexedAt = DateTime.UtcNow;
                    // Note: CreatedAt should not be updated for existing stories
                }
                
                _logger.LogDebug("Updating {Count} existing stories in index", storiesToUpdate.Count);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully bulk indexed {Count} stories ({NewCount} new, {UpdatedCount} updated)", 
                storyList.Count, newStories.Count, storiesToUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk indexing stories");
            throw;
        }
    }

    public async Task<bool> IsStoryIndexedAsync(int storyId)
    {
        return await _context.Stories.AnyAsync(s => s.Id == storyId);
    }

    public async Task RebuildIndexAsync()
    {
        try
        {
            _logger.LogInformation("Starting FTS index rebuild");
            await _context.RebuildFtsIndexAsync();
            _logger.LogInformation("FTS index rebuild completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding FTS index");
            throw;
        }
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
            // Note: CreatedAt, HasUrl, CommentCount, and HackerNewsUrl are computed properties
            // They are automatically calculated from the above properties
        };
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