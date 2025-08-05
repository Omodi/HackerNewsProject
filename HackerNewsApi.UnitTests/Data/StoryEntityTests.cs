using FluentAssertions;
using HackerNewsApi.Infrastructure.Data;

namespace HackerNewsApi.UnitTests.Data;

public class StoryEntityTests
{
    [Fact]
    public void StoryEntity_DefaultValues_ShouldBeSetCorrectly()
    {
        
        var entity = new StoryEntity();

                entity.Id.Should().Be(0);
        entity.Title.Should().Be(string.Empty);
        entity.Author.Should().Be(string.Empty);
        entity.Url.Should().BeNull();
        entity.Score.Should().Be(0);
        entity.CreatedAt.Should().Be(default(DateTime));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.CommentCount.Should().Be(0);
        entity.Domain.Should().BeNull();
        entity.IndexedAt.Should().BeNull();
    }

    [Fact]
    public void StoryEntity_WithValidData_ShouldSetPropertiesCorrectly()
    {
        
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;
        var indexedAt = DateTime.UtcNow.AddMinutes(-30);

        
        var entity = new StoryEntity
        {
            Id = 123,
            Title = "Test Story Title",
            Author = "testuser",
            Url = "https://example.com/story",
            Score = 150,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            CommentCount = 25,
            Domain = "example.com",
            IndexedAt = indexedAt
        };

                entity.Id.Should().Be(123);
        entity.Title.Should().Be("Test Story Title");
        entity.Author.Should().Be("testuser");
        entity.Url.Should().Be("https://example.com/story");
        entity.Score.Should().Be(150);
        entity.CreatedAt.Should().Be(createdAt);
        entity.UpdatedAt.Should().Be(updatedAt);
        entity.CommentCount.Should().Be(25);
        entity.Domain.Should().Be("example.com");
        entity.IndexedAt.Should().Be(indexedAt);
    }

    [Fact]
    public void StoryEntity_HasUrl_WithValidUrl_ShouldReturnTrue()
    {
        
        var entity = new StoryEntity
        {
            Url = "https://example.com/story"
        };

        
        entity.HasUrl.Should().BeTrue();
    }

    [Fact]
    public void StoryEntity_HasUrl_WithEmptyUrl_ShouldReturnFalse()
    {
        
        var entity = new StoryEntity
        {
            Url = ""
        };

        
        entity.HasUrl.Should().BeFalse();
    }

    [Fact]
    public void StoryEntity_HasUrl_WithNullUrl_ShouldReturnFalse()
    {
        
        var entity = new StoryEntity
        {
            Url = null
        };

        
        entity.HasUrl.Should().BeFalse();
    }

    [Fact]
    public void StoryEntity_HackerNewsUrl_ShouldGenerateCorrectUrl()
    {
        
        var entity = new StoryEntity
        {
            Id = 123
        };

        
        var url = entity.HackerNewsUrl;

                url.Should().Be("https://news.ycombinator.com/item?id=123");
    }

    [Theory]
    [InlineData(1, "https://news.ycombinator.com/item?id=1")]
    [InlineData(999999, "https://news.ycombinator.com/item?id=999999")]
    [InlineData(0, "https://news.ycombinator.com/item?id=0")]
    public void StoryEntity_HackerNewsUrl_WithDifferentIds_ShouldGenerateCorrectUrls(int id, string expectedUrl)
    {
        
        var entity = new StoryEntity { Id = id };

        
        var url = entity.HackerNewsUrl;

                url.Should().Be(expectedUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Short title")]
    [InlineData("Very long title that might contain special characters and numbers 123 !@#$%^&*()")]
    public void StoryEntity_TitleProperty_ShouldAcceptVariousValues(string title)
    {
        
        var entity = new StoryEntity { Title = title };

                entity.Title.Should().Be(title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("user")]
    [InlineData("user_with_underscore")]
    [InlineData("very-long-username-with-special-chars-123")]
    public void StoryEntity_AuthorProperty_ShouldAcceptVariousValues(string author)
    {
        
        var entity = new StoryEntity { Author = author };

                entity.Author.Should().Be(author);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://example.com")]
    [InlineData("https://very-long-domain-name.example.org/path/to/resource?param=value&another=value")]
    public void StoryEntity_UrlProperty_ShouldAcceptVariousValues(string? url)
    {
        
        var entity = new StoryEntity { Url = url };

                entity.Url.Should().Be(url);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void StoryEntity_ScoreProperty_ShouldAcceptValidValues(int score)
    {
        
        var entity = new StoryEntity { Score = score };

                entity.Score.Should().Be(score);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(500)]
    [InlineData(int.MaxValue)]
    public void StoryEntity_CommentCountProperty_ShouldAcceptValidValues(int commentCount)
    {
        
        var entity = new StoryEntity { CommentCount = commentCount };

                entity.CommentCount.Should().Be(commentCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("example.com")]
    [InlineData("subdomain.example.com")]
    [InlineData("very-long-domain-name.example.org")]
    public void StoryEntity_DomainProperty_ShouldAcceptVariousValues(string? domain)
    {
        
        var entity = new StoryEntity { Domain = domain };

                entity.Domain.Should().Be(domain);
    }

    [Fact]
    public void StoryEntity_DateTimeProperties_ShouldAcceptDateTimeValues()
    {
        
        var createdAt = new DateTime(2023, 6, 15, 10, 30, 45);
        var updatedAt = new DateTime(2023, 6, 15, 11, 30, 45);
        var indexedAt = new DateTime(2023, 6, 15, 12, 30, 45);

        
        var entity = new StoryEntity
        {
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IndexedAt = indexedAt
        };

                entity.CreatedAt.Should().Be(createdAt);
        entity.UpdatedAt.Should().Be(updatedAt);
        entity.IndexedAt.Should().Be(indexedAt);
    }

    [Fact]
    public void StoryEntity_PropertyModification_ShouldUpdateCorrectly()
    {
        
        var entity = new StoryEntity
        {
            Id = 1,
            Title = "Original Title",
            Score = 50,
            Url = "https://original.com"
        };

        
        entity.Id = 2;
        entity.Title = "Updated Title";
        entity.Score = 100;
        entity.Url = "https://updated.com";

                entity.Id.Should().Be(2);
        entity.Title.Should().Be("Updated Title");
        entity.Score.Should().Be(100);
        entity.Url.Should().Be("https://updated.com");
        
        // Computed properties should update accordingly
        entity.HasUrl.Should().BeTrue();
        entity.HackerNewsUrl.Should().Be("https://news.ycombinator.com/item?id=2");
    }

    [Fact]
    public void StoryEntity_ComputedProperties_ShouldBeReadOnly()
    {
        
        var entity = new StoryEntity { Id = 123, Url = "https://example.com" };

        
        // These should be computed properties and cannot be set directly
        entity.HasUrl.Should().BeTrue();
        entity.HackerNewsUrl.Should().Be("https://news.ycombinator.com/item?id=123");
    }

    [Fact]
    public void StoryEntity_UpdatedAt_DefaultValue_ShouldBeRecentUtcTime()
    {
        
        var entity = new StoryEntity();

                entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }
}