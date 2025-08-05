using System.Text.Json.Serialization;

namespace HackerNewsApi.Core.Models;

public class Story
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("by")]
    public string By { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "story";

    [JsonPropertyName("kids")]
    public int[]? Kids { get; set; }

    [JsonPropertyName("descendants")]
    public int? Descendants { get; set; }

    /// Computed property for human-readable creation date
    public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(Time).DateTime;

    public bool HasUrl => !string.IsNullOrEmpty(Url);

    /// Number of comments on this story
    public int CommentCount => Descendants ?? 0;

    /// HackerNews URL for viewing comments
    public string HackerNewsUrl => $"https://news.ycombinator.com/item?id={Id}";
}