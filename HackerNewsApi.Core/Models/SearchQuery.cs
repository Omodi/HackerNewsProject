using System.ComponentModel.DataAnnotations;

namespace HackerNewsApi.Core.Models;

public class SearchQuery
{
    [Required]
    [MinLength(1)]
    public string Query { get; set; } = string.Empty;
    
    public SearchFilters? Filters { get; set; }
    
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
    
    public SearchSortOrder SortBy { get; set; } = SearchSortOrder.Relevance;
}

public class SearchFilters
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    [Range(0, int.MaxValue)]
    public int? MinScore { get; set; }
    
    [Range(0, int.MaxValue)]
    public int? MaxScore { get; set; }
    
    [MaxLength(100)]
    public string? Author { get; set; }
    
    [MaxLength(200)]
    public string? Domain { get; set; }
    
    public bool? HasUrl { get; set; }
}

public enum SearchSortOrder
{
    Relevance,
    Score,
    Recent,
    Oldest,
    Comments
}

public class SearchStatistics
{
    public int TotalIndexedStories { get; set; }
    public DateTime? LastIndexedAt { get; set; }
    public DateTime? OldestStoryDate { get; set; }
    public DateTime? NewestStoryDate { get; set; }
    public int SearchesPerformed { get; set; }
    public string? MostPopularTerm { get; set; }
}