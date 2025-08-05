using System.ComponentModel.DataAnnotations;

namespace HackerNewsApi.Infrastructure.Data;

public class StoryEntity
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Author { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Url { get; set; }
    
    public int Score { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int CommentCount { get; set; }
    
    [MaxLength(200)]
    public string? Domain { get; set; }
    
    public DateTime? IndexedAt { get; set; }
    
    // Computed properties (not mapped to database)
    public bool HasUrl => !string.IsNullOrEmpty(Url);
    public string HackerNewsUrl => $"https://news.ycombinator.com/item?id={Id}";
}

public class SearchResultDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Url { get; set; }
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CommentCount { get; set; }
    public string? Domain { get; set; }
    public double Relevance { get; set; }
}