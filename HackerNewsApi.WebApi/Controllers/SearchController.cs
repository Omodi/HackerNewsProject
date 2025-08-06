using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchRepository _searchRepository;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchRepository searchRepository, ILogger<SearchController> logger)
    {
        _searchRepository = searchRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<Story>>> SearchAsync(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SearchSortOrder sortBy = SearchSortOrder.Relevance,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? minScore = null,
        [FromQuery] int? maxScore = null,
        [FromQuery] string? author = null,
        [FromQuery] string? domain = null,
        [FromQuery] bool? hasUrl = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var searchQuery = new SearchQuery
            {
                Query = q ?? string.Empty,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                Filters = new SearchFilters
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    MinScore = minScore,
                    MaxScore = maxScore,
                    Author = author,
                    Domain = domain,
                    HasUrl = hasUrl
                }
            };

            _logger.LogInformation("Enhanced search request: Query='{Query}', Page={Page}, PageSize={PageSize}, SortBy={SortBy}", 
                q, page, pageSize, sortBy);

            var result = await _searchRepository.SearchStoriesAsync(searchQuery);
            
            _logger.LogDebug("Search returned {Count} results",
                result.Items?.Count() ?? 0);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing enhanced search");
            return StatusCode(500, new { error = "An error occurred while searching stories" });
        }
    }

    /// <summary>
    /// Get search suggestions based on partial query
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<IEnumerable<string>>> GetSuggestionsAsync(
        [FromQuery] string q,
        [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Ok(Array.Empty<string>());
            }

            if (limit < 1) limit = 10;
            if (limit > 20) limit = 20;

            var suggestions = await _searchRepository.GetSearchSuggestionsAsync(q, limit);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query: {Query}", q);
            return StatusCode(500, new { error = "An error occurred while getting suggestions" });
        }
    }

    /// <summary>
    /// Rebuild the full-text search index (admin operation)
    /// </summary>
    [HttpPost("rebuild-index")]
    public async Task<ActionResult> RebuildIndexAsync()
    {
        try
        {
            _logger.LogInformation("Manual search index rebuild requested");
            await _searchRepository.RebuildIndexAsync();
            _logger.LogInformation("Search index rebuild completed");
            
            return Ok(new { message = "Search index has been rebuilt successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding search index");
            return StatusCode(500, new { error = "An error occurred while rebuilding the search index" });
        }
    }
}