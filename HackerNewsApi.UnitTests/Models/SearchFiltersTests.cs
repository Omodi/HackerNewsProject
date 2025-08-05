using FluentAssertions;
using HackerNewsApi.Core.Models;

namespace HackerNewsApi.UnitTests.Models;

public class SearchFiltersTests
{
    [Fact]
    public void SearchFilters_DefaultValues_ShouldBeNull()
    {
        // Act
        var filters = new SearchFilters();

        // Assert
        filters.FromDate.Should().BeNull();
        filters.ToDate.Should().BeNull();
        filters.MinScore.Should().BeNull();
        filters.MaxScore.Should().BeNull();
        filters.Author.Should().BeNull();
        filters.Domain.Should().BeNull();
        filters.HasUrl.Should().BeNull();
    }

    [Fact]
    public void SearchFilters_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        // Act
        var filters = new SearchFilters
        {
            FromDate = fromDate,
            ToDate = toDate,
            MinScore = 10,
            MaxScore = 100,
            Author = "testuser",
            Domain = "example.com",
            HasUrl = true
        };

        // Assert
        filters.FromDate.Should().Be(fromDate);
        filters.ToDate.Should().Be(toDate);
        filters.MinScore.Should().Be(10);
        filters.MaxScore.Should().Be(100);
        filters.Author.Should().Be("testuser");
        filters.Domain.Should().Be("example.com");
        filters.HasUrl.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void SearchFilters_ScoreProperties_ShouldAcceptValidValues(int score)
    {
        // Act
        var filters = new SearchFilters
        {
            MinScore = score,
            MaxScore = score
        };

        // Assert
        filters.MinScore.Should().Be(score);
        filters.MaxScore.Should().Be(score);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("validuser")]
    [InlineData("user_with_underscore")]
    public void SearchFilters_AuthorProperty_ShouldAcceptVariousValues(string author)
    {
        // Act
        var filters = new SearchFilters { Author = author };

        // Assert
        filters.Author.Should().Be(author);
    }

    [Theory]
    [InlineData("")]
    [InlineData("example.com")]
    [InlineData("subdomain.example.com")]
    [InlineData("very-long-domain-name.example.org")]
    public void SearchFilters_DomainProperty_ShouldAcceptVariousValues(string domain)
    {
        // Act
        var filters = new SearchFilters { Domain = domain };

        // Assert
        filters.Domain.Should().Be(domain);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SearchFilters_HasUrlProperty_ShouldAcceptBooleanValues(bool hasUrl)
    {
        // Act
        var filters = new SearchFilters { HasUrl = hasUrl };

        // Assert
        filters.HasUrl.Should().Be(hasUrl);
    }

    [Fact]
    public void SearchFilters_DateProperties_ShouldAcceptDateTimeValues()
    {
        // Arrange
        var fromDate = new DateTime(2023, 1, 1);
        var toDate = new DateTime(2023, 12, 31);

        // Act
        var filters = new SearchFilters
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        // Assert
        filters.FromDate.Should().Be(fromDate);
        filters.ToDate.Should().Be(toDate);
    }
}