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

    /// Computed property for human-readable creation date
    public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(Time).DateTime;

    /// Whether this story has a URL (vs being a text post)
    public bool HasUrl => !string.IsNullOrEmpty(Url);
}