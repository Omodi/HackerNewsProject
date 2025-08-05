using FluentAssertions;
using HackerNewsApi.Infrastructure.Data;

namespace HackerNewsApi.UnitTests.Data;

public class SearchResultDtoTests
{
    [Fact]
    public void SearchResultDto_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var dto = new SearchResultDto();

        // Assert
        dto.Id.Should().Be(0);
        dto.Title.Should().Be(string.Empty);
        dto.Author.Should().Be(string.Empty);
        dto.Url.Should().BeNull();
        dto.Score.Should().Be(0);
        dto.CreatedAt.Should().Be(default(DateTime));
        dto.CommentCount.Should().Be(0);
        dto.Domain.Should().BeNull();
        dto.Relevance.Should().Be(0.0);
    }

    [Fact]
    public void SearchResultDto_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;

        // Act
        var dto = new SearchResultDto
        {
            Id = 12345,
            Title = "Test Story Title",
            Author = "testauthor",
            Url = "https://example.com/story",
            Score = 150,
            CreatedAt = createdAt,
            CommentCount = 25,
            Domain = "example.com",
            Relevance = 0.95
        };

        // Assert
        dto.Id.Should().Be(12345);
        dto.Title.Should().Be("Test Story Title");
        dto.Author.Should().Be("testauthor");
        dto.Url.Should().Be("https://example.com/story");
        dto.Score.Should().Be(150);
        dto.CreatedAt.Should().Be(createdAt);
        dto.CommentCount.Should().Be(25);
        dto.Domain.Should().Be("example.com");
        dto.Relevance.Should().Be(0.95);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Simple Title")]
    [InlineData("Title with Special Characters: @#$%^&*()")]
    public void SearchResultDto_Title_ShouldAcceptVariousValues(string title)
    {
        // Act
        var dto = new SearchResultDto { Title = title };

        // Assert
        dto.Title.Should().Be(title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("user")]
    [InlineData("user_with_underscore")]
    public void SearchResultDto_Author_ShouldAcceptVariousValues(string author)
    {
        // Act
        var dto = new SearchResultDto { Author = author };

        // Assert
        dto.Author.Should().Be(author);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://example.com")]
    public void SearchResultDto_Url_ShouldAcceptVariousValues(string? url)
    {
        // Act
        var dto = new SearchResultDto { Url = url };

        // Assert
        dto.Url.Should().Be(url);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1000)]
    public void SearchResultDto_Score_ShouldAcceptVariousValues(int score)
    {
        // Act
        var dto = new SearchResultDto { Score = score };

        // Assert
        dto.Score.Should().Be(score);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void SearchResultDto_Relevance_ShouldAcceptVariousValues(double relevance)
    {
        // Act
        var dto = new SearchResultDto { Relevance = relevance };

        // Assert
        dto.Relevance.Should().Be(relevance);
    }
}