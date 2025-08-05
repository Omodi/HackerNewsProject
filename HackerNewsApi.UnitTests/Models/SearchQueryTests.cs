using FluentAssertions;
using HackerNewsApi.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace HackerNewsApi.UnitTests.Models;

public class SearchQueryTests
{
    [Fact]
    public void SearchQuery_DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var query = new SearchQuery();

        // Assert
        query.Query.Should().Be(string.Empty);
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
        query.SortBy.Should().Be(SearchSortOrder.Relevance);
        query.Filters.Should().BeNull();
    }

    [Fact]
    public void SearchQuery_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var filters = new SearchFilters
        {
            MinScore = 10,
            MaxScore = 100,
            Author = "testuser"
        };

        // Act
        var query = new SearchQuery
        {
            Query = "test query",
            Page = 2,
            PageSize = 50,
            SortBy = SearchSortOrder.Score,
            Filters = filters
        };

        // Assert
        query.Query.Should().Be("test query");
        query.Page.Should().Be(2);
        query.PageSize.Should().Be(50);
        query.SortBy.Should().Be(SearchSortOrder.Score);
        query.Filters.Should().Be(filters);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("valid query")]
    [InlineData("very long query that exceeds normal length")]
    public void SearchQuery_QueryProperty_ShouldAcceptVariousValues(string queryValue)
    {
        // Act
        var query = new SearchQuery { Query = queryValue };

        // Assert
        query.Query.Should().Be(queryValue);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void SearchQuery_PageProperty_ShouldAcceptValidValues(int pageValue)
    {
        // Act
        var query = new SearchQuery { Page = pageValue };

        // Assert
        query.Page.Should().Be(pageValue);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(100)]
    public void SearchQuery_PageSizeProperty_ShouldAcceptValidValues(int pageSizeValue)
    {
        // Act
        var query = new SearchQuery { PageSize = pageSizeValue };

        // Assert
        query.PageSize.Should().Be(pageSizeValue);
    }

    [Theory]
    [InlineData(SearchSortOrder.Relevance)]
    [InlineData(SearchSortOrder.Score)]
    [InlineData(SearchSortOrder.Recent)]
    [InlineData(SearchSortOrder.Oldest)]
    [InlineData(SearchSortOrder.Comments)]
    public void SearchQuery_SortByProperty_ShouldAcceptAllEnumValues(SearchSortOrder sortOrder)
    {
        // Act
        var query = new SearchQuery { SortBy = sortOrder };

        // Assert
        query.SortBy.Should().Be(sortOrder);
    }

    [Fact]
    public void SearchQuery_ValidationAttributes_ShouldBeAppliedCorrectly()
    {
        // Arrange
        var query = new SearchQuery();
        var context = new ValidationContext(query);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(query, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle(r => r.MemberNames.Contains("Query"));
    }
}

public class SearchSortOrderTests
{
    [Fact]
    public void SearchSortOrder_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<SearchSortOrder>().Should().Contain(new[]
        {
            SearchSortOrder.Relevance,
            SearchSortOrder.Score,
            SearchSortOrder.Recent,
            SearchSortOrder.Oldest,
            SearchSortOrder.Comments
        });
    }

    [Fact]
    public void SearchSortOrder_DefaultValue_ShouldBeRelevance()
    {
        // Act
        var defaultValue = default(SearchSortOrder);

        // Assert
        defaultValue.Should().Be(SearchSortOrder.Relevance);
    }

    [Theory]
    [InlineData(SearchSortOrder.Relevance, "Relevance")]
    [InlineData(SearchSortOrder.Score, "Score")]
    [InlineData(SearchSortOrder.Recent, "Recent")]
    [InlineData(SearchSortOrder.Oldest, "Oldest")]
    [InlineData(SearchSortOrder.Comments, "Comments")]
    public void SearchSortOrder_ToString_ShouldReturnCorrectName(SearchSortOrder sortOrder, string expectedName)
    {
        // Act
        var result = sortOrder.ToString();

        // Assert
        result.Should().Be(expectedName);
    }
}

public class SearchStatisticsTests
{
    [Fact]
    public void SearchStatistics_DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var stats = new SearchStatistics();

        // Assert
        stats.TotalIndexedStories.Should().Be(0);
        stats.LastIndexedAt.Should().BeNull();
        stats.OldestStoryDate.Should().BeNull();
        stats.NewestStoryDate.Should().BeNull();
        stats.SearchesPerformed.Should().Be(0);
        stats.MostPopularTerm.Should().BeNull();
    }

    [Fact]
    public void SearchStatistics_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var lastIndexed = DateTime.UtcNow;
        var oldestDate = DateTime.UtcNow.AddYears(-1);
        var newestDate = DateTime.UtcNow;

        // Act
        var stats = new SearchStatistics
        {
            TotalIndexedStories = 1000,
            LastIndexedAt = lastIndexed,
            OldestStoryDate = oldestDate,
            NewestStoryDate = newestDate,
            SearchesPerformed = 5000,
            MostPopularTerm = "javascript"
        };

        // Assert
        stats.TotalIndexedStories.Should().Be(1000);
        stats.LastIndexedAt.Should().Be(lastIndexed);
        stats.OldestStoryDate.Should().Be(oldestDate);
        stats.NewestStoryDate.Should().Be(newestDate);
        stats.SearchesPerformed.Should().Be(5000);
        stats.MostPopularTerm.Should().Be("javascript");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void SearchStatistics_CountProperties_ShouldAcceptValidValues(int count)
    {
        // Act
        var stats = new SearchStatistics
        {
            TotalIndexedStories = count,
            SearchesPerformed = count
        };

        // Assert
        stats.TotalIndexedStories.Should().Be(count);
        stats.SearchesPerformed.Should().Be(count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("javascript")]
    [InlineData("react")]
    [InlineData("very long search term that users might type")]
    public void SearchStatistics_MostPopularTerm_ShouldAcceptVariousValues(string term)
    {
        // Act
        var stats = new SearchStatistics { MostPopularTerm = term };

        // Assert
        stats.MostPopularTerm.Should().Be(term);
    }

    [Fact]
    public void SearchStatistics_DateProperties_ShouldAcceptDateTimeValues()
    {
        // Arrange
        var lastIndexed = DateTime.UtcNow;
        var oldestDate = DateTime.UtcNow.AddYears(-2);
        var newestDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var stats = new SearchStatistics
        {
            LastIndexedAt = lastIndexed,
            OldestStoryDate = oldestDate,
            NewestStoryDate = newestDate
        };

        // Assert
        stats.LastIndexedAt.Should().Be(lastIndexed);
        stats.OldestStoryDate.Should().Be(oldestDate);
        stats.NewestStoryDate.Should().Be(newestDate);
    }
}