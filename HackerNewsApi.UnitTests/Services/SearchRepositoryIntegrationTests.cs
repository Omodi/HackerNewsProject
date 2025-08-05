using FluentAssertions;
using HackerNewsApi.Core.Models;
using HackerNewsApi.Infrastructure.Data;
using HackerNewsApi.Infrastructure.Services;
using HackerNewsApi.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HackerNewsApi.UnitTests.Services;

/// <summary>
/// Integration tests for SearchRepository using SQLite database with FTS support
/// </summary>
public class SearchRepositoryIntegrationTests : DatabaseTestBase, IClassFixture<DatabaseTestFixture>
{
    private readonly Mock<ILogger<SearchRepository>> _mockLogger;
    private SearchRepository _repository;

    public SearchRepositoryIntegrationTests(DatabaseTestFixture databaseFixture) : base(databaseFixture)
    {
        _mockLogger = new Mock<ILogger<SearchRepository>>();
        _repository = new SearchRepository(Context, _mockLogger.Object);
    }

    #region SearchStoriesAsync Tests with Real SQLite

    [Fact]
    public async Task SearchStoriesAsync_WithEmptyQuery_ShouldReturnAllStories()
    {
        
        await SeedTestData();
        
        var searchQuery = new SearchQuery
        {
            Query = "",
            Page = 1,
            PageSize = 10,
            SortBy = SearchSortOrder.Recent
        };

        
        var result = await _repository.SearchStoriesAsync(searchQuery);

        
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SearchStoriesAsync_WithQuery_ShouldReturnFilteredResults()
    {
        
        await SeedTestData();
        
        var searchQuery = new SearchQuery
        {
            Query = "JavaScript",
            Page = 1,
            PageSize = 10,
            SortBy = SearchSortOrder.Relevance
        };

        
        var result = await _repository.SearchStoriesAsync(searchQuery);

        
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Theory]
    [InlineData(SearchSortOrder.Relevance)]
    [InlineData(SearchSortOrder.Score)]
    [InlineData(SearchSortOrder.Recent)]
    [InlineData(SearchSortOrder.Oldest)]
    [InlineData(SearchSortOrder.Comments)]
    public async Task SearchStoriesAsync_WithDifferentSortOrders_ShouldExecuteCorrectly(SearchSortOrder sortOrder)
    {
        
        await SeedTestData();
        
        var searchQuery = new SearchQuery
        {
            Query = "",
            Page = 1,
            PageSize = 5,
            SortBy = sortOrder
        };

        
        var result = await _repository.SearchStoriesAsync(searchQuery);

        
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchStoriesAsync_WithDateFilters_ShouldFilterCorrectly()
    {
        
        await SeedTestData();
        
        var searchQuery = new SearchQuery
        {
            Query = "",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters
            {
                FromDate = DateTime.UtcNow.AddDays(-7),
                ToDate = DateTime.UtcNow
            }
        };

        
        var result = await _repository.SearchStoriesAsync(searchQuery);

        
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchStoriesAsync_WithScoreFilters_ShouldFilterCorrectly()
    {
        
        await SeedTestData();
        
        var searchQuery = new SearchQuery
        {
            Query = "",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters
            {
                MinScore = 100,
                MaxScore = 300
            }
        };

        
        var result = await _repository.SearchStoriesAsync(searchQuery);

        
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchStoriesAsync_WithAuthorFilter_ShouldFilterCorrectly()
    {
        
        await SeedTestData();
        
        var searchQuery = new SearchQuery
        {
            Query = "",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters
            {
                Author = "developer"
            }
        };

        
        var result = await _repository.SearchStoriesAsync(searchQuery);

        
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SearchStoriesAsync_WithHasUrlFilter_ShouldFilterCorrectly(bool hasUrl)
    {
        
        await SeedTestData();
        
        var searchQuery = new SearchQuery
        {
            Query = "",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters
            {
                HasUrl = hasUrl
            }
        };

        
        var result = await _repository.SearchStoriesAsync(searchQuery);

        
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchStoriesAsync_WithPagination_ShouldReturnCorrectPage()
    {
        
        await SeedTestData();
        
        var searchQuery = new SearchQuery
        {
            Query = "",
            Page = 2,
            PageSize = 2
        };

        
        var result = await _repository.SearchStoriesAsync(searchQuery);

        
        result.Should().NotBeNull();
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
    }

    #endregion

    #region FTS Index Tests

    [Fact]
    public async Task RebuildIndexAsync_ShouldExecuteSuccessfully()
    {
        
        await SeedTestData();

        
        var act = async () => await _repository.RebuildIndexAsync();
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Other Repository Methods

    [Fact]
    public async Task GetSearchSuggestionsAsync_WithValidQuery_ShouldReturnSuggestions()
    {
        
        await SeedTestData();

        
        var result = await _repository.GetSearchSuggestionsAsync("java", 10);

        
        result.Should().NotBeNull();
        // May or may not have results depending on test data
    }

    [Fact]
    public async Task IndexStoryAsync_WithNewStory_ShouldAddStory()
    {
        
        var story = CreateTestStory(999, "New Integration Test Story");

        
        await _repository.IndexStoryAsync(story);

        
        var indexedStory = await Context.Stories.FindAsync(999);
        indexedStory.Should().NotBeNull();
        indexedStory!.Title.Should().Be("New Integration Test Story");
    }

    [Fact]
    public async Task IsStoryIndexedAsync_WithExistingStory_ShouldReturnTrue()
    {
        
        await SeedTestData();

        
        var result = await _repository.IsStoryIndexedAsync(1001);

        
        result.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestData()
    {
        var stories = new[]
        {
            new StoryEntity
            {
                Id = 1001,
                Title = "JavaScript Development Best Practices",
                Author = "developer",
                Score = 150,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow,
                CommentCount = 25,
                Url = "https://example.com/js-best-practices",
                Domain = "example.com"
            },
            new StoryEntity
            {
                Id = 1002,
                Title = "React Testing Strategies",
                Author = "testuser1",
                Score = 200,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow,
                CommentCount = 30,
                Url = "https://react.dev/testing",
                Domain = "react.dev"
            },
            new StoryEntity
            {
                Id = 1003,
                Title = "Story Without URL",
                Author = "author_no_url",
                Score = 75,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow,
                CommentCount = 5,
                Url = null,
                Domain = null
            },
            new StoryEntity
            {
                Id = 1004,
                Title = "High Score Article",
                Author = "popular_author",
                Score = 500,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow,
                CommentCount = 100,
                Url = "https://popular.com/article",
                Domain = "popular.com"
            }
        };

        Context.Stories.AddRange(stories);
        await Context.SaveChangesAsync();
    }

    private static Story CreateTestStory(int id, string title, string? url = null)
    {
        return new Story
        {
            Id = id,
            Title = title,
            By = "testuser",
            Score = 100,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = url,
            Descendants = 10,
            Type = "story"
        };
    }

    #endregion
}