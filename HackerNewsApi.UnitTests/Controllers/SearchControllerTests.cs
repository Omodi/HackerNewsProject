using FluentAssertions;
using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using HackerNewsApi.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsApi.UnitTests.Controllers;

public class SearchControllerTests
{
    private readonly Mock<ISearchRepository> _mockSearchRepository;
    private readonly Mock<ILogger<SearchController>> _mockLogger;
    private readonly SearchController _controller;

    public SearchControllerTests()
    {
        _mockSearchRepository = new Mock<ISearchRepository>();
        _mockLogger = new Mock<ILogger<SearchController>>();
        _controller = new SearchController(_mockSearchRepository.Object, _mockLogger.Object);
    }

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithValidQuery_ShouldReturnOkResult()
    {
        
        var expectedResult = new PagedResult<Story>
        {
            Items = new[]
            {
                new Story { Id = 1, Title = "Test Story 1", By = "user1" },
                new Story { Id = 2, Title = "Test Story 2", By = "user2" }
            },
            Page = 1,
            PageSize = 20,
            TotalCount = 2
        };

        _mockSearchRepository.Setup(x => x.SearchStoriesAsync(It.IsAny<SearchQuery>()))
                             .ReturnsAsync(expectedResult);

        
        var result = await _controller.SearchAsync("test query");

                result.Should().BeOfType<ActionResult<PagedResult<Story>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedValue = okResult.Value.Should().BeOfType<PagedResult<Story>>().Subject;
        returnedValue.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ShouldReturnOkResult()
    {
        
        var expectedResult = new PagedResult<Story>
        {
            Items = new[]
            {
                new Story { Id = 1, Title = "Recent Story 1" },
                new Story { Id = 2, Title = "Recent Story 2" }
            },
            Page = 1,
            PageSize = 20,
            TotalCount = 2
        };

        _mockSearchRepository.Setup(x => x.SearchStoriesAsync(It.IsAny<SearchQuery>()))
                             .ReturnsAsync(expectedResult);

        
        var result = await _controller.SearchAsync(null);

                result.Result.Should().BeOfType<OkObjectResult>();
        _mockSearchRepository.Verify(x => x.SearchStoriesAsync(It.Is<SearchQuery>(q => q.Query == string.Empty)), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithInvalidPage_ShouldNormalizePage()
    {
        
        var expectedResult = new PagedResult<Story> { Items = Array.Empty<Story>(), TotalCount = 0 };
        _mockSearchRepository.Setup(x => x.SearchStoriesAsync(It.IsAny<SearchQuery>()))
                             .ReturnsAsync(expectedResult);

        
        var result = await _controller.SearchAsync("test", page: -5);

                _mockSearchRepository.Verify(x => x.SearchStoriesAsync(It.Is<SearchQuery>(q => q.Page == 1)), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithInvalidPageSize_ShouldNormalizePageSize()
    {
        
        var expectedResult = new PagedResult<Story> { Items = Array.Empty<Story>(), TotalCount = 0 };
        _mockSearchRepository.Setup(x => x.SearchStoriesAsync(It.IsAny<SearchQuery>()))
                             .ReturnsAsync(expectedResult);

        
        var result = await _controller.SearchAsync("test", pageSize: 0);

                _mockSearchRepository.Verify(x => x.SearchStoriesAsync(It.Is<SearchQuery>(q => q.PageSize == 20)), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithAllFilters_ShouldPassFiltersCorrectly()
    {
        
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;
        var expectedResult = new PagedResult<Story> { Items = Array.Empty<Story>(), TotalCount = 0 };

        _mockSearchRepository.Setup(x => x.SearchStoriesAsync(It.IsAny<SearchQuery>()))
                             .ReturnsAsync(expectedResult);

        
        await _controller.SearchAsync(
            "test",
            page: 2,
            pageSize: 10,
            sortBy: SearchSortOrder.Score,
            fromDate: fromDate,
            toDate: toDate,
            minScore: 10,
            maxScore: 100,
            author: "testuser",
            domain: "example.com",
            hasUrl: true);

                _mockSearchRepository.Verify(x => x.SearchStoriesAsync(It.Is<SearchQuery>(q =>
            q.Query == "test" &&
            q.Page == 2 &&
            q.PageSize == 10 &&
            q.SortBy == SearchSortOrder.Score &&
            q.Filters != null &&
            q.Filters.FromDate == fromDate &&
            q.Filters.ToDate == toDate &&
            q.Filters.MinScore == 10 &&
            q.Filters.MaxScore == 100 &&
            q.Filters.Author == "testuser" &&
            q.Filters.Domain == "example.com" &&
            q.Filters.HasUrl == true
        )), Times.Once);
    }

    [Theory]
    [InlineData(SearchSortOrder.Relevance)]
    [InlineData(SearchSortOrder.Score)]
    [InlineData(SearchSortOrder.Recent)]
    [InlineData(SearchSortOrder.Oldest)]
    [InlineData(SearchSortOrder.Comments)]
    public async Task SearchAsync_WithDifferentSortOrders_ShouldPassSortOrderCorrectly(SearchSortOrder sortOrder)
    {
        
        var expectedResult = new PagedResult<Story> { Items = Array.Empty<Story>(), TotalCount = 0 };
        _mockSearchRepository.Setup(x => x.SearchStoriesAsync(It.IsAny<SearchQuery>()))
                             .ReturnsAsync(expectedResult);

        
        await _controller.SearchAsync("test", sortBy: sortOrder);

                _mockSearchRepository.Verify(x => x.SearchStoriesAsync(It.Is<SearchQuery>(q => q.SortBy == sortOrder)), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WhenRepositoryThrows_ShouldReturn500()
    {
        
        _mockSearchRepository.Setup(x => x.SearchStoriesAsync(It.IsAny<SearchQuery>()))
                             .ThrowsAsync(new Exception("Database error"));

        
        var result = await _controller.SearchAsync("test");

                var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().BeEquivalentTo(new { error = "An error occurred while searching stories" });
    }

    #endregion

    #region GetSuggestionsAsync Tests

    [Fact]
    public async Task GetSuggestionsAsync_WithValidQuery_ShouldReturnOkResult()
    {
        
        var expectedSuggestions = new[] { "test suggestion 1", "test suggestion 2" };
        _mockSearchRepository.Setup(x => x.GetSearchSuggestionsAsync("te", 10))
                             .ReturnsAsync(expectedSuggestions);

        
        var result = await _controller.GetSuggestionsAsync("te");

                result.Should().BeOfType<ActionResult<IEnumerable<string>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSuggestions = okResult.Value.Should().BeAssignableTo<IEnumerable<string>>().Subject;
        returnedSuggestions.Should().BeEquivalentTo(expectedSuggestions);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WithEmptyQuery_ShouldReturnEmptyArray()
    {
        
        var result = await _controller.GetSuggestionsAsync("");

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSuggestions = okResult.Value.Should().BeAssignableTo<IEnumerable<string>>().Subject;
        returnedSuggestions.Should().BeEmpty();
        
        // Verify repository was not called
        _mockSearchRepository.Verify(x => x.GetSearchSuggestionsAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WithWhitespaceQuery_ShouldReturnEmptyArray()
    {
        
        var result = await _controller.GetSuggestionsAsync("   ");

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSuggestions = okResult.Value.Should().BeAssignableTo<IEnumerable<string>>().Subject;
        returnedSuggestions.Should().BeEmpty();
        
        // Verify repository was not called
        _mockSearchRepository.Verify(x => x.GetSearchSuggestionsAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WithSingleCharacterQuery_ShouldReturnEmptyArray()
    {
        
        var result = await _controller.GetSuggestionsAsync("a");

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSuggestions = okResult.Value.Should().BeAssignableTo<IEnumerable<string>>().Subject;
        returnedSuggestions.Should().BeEmpty();
        
        // Verify repository was not called
        _mockSearchRepository.Verify(x => x.GetSearchSuggestionsAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WithInvalidLimit_ShouldNormalizeLimit()
    {
        
        var expectedSuggestions = new[] { "suggestion" };
        _mockSearchRepository.Setup(x => x.GetSearchSuggestionsAsync("test", 10))
                             .ReturnsAsync(expectedSuggestions);

        
        var result = await _controller.GetSuggestionsAsync("test", limit: 0);

                _mockSearchRepository.Verify(x => x.GetSearchSuggestionsAsync("test", 10), Times.Once);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WithLimitTooLarge_ShouldCapLimit()
    {
        
        var expectedSuggestions = new[] { "suggestion" };
        _mockSearchRepository.Setup(x => x.GetSearchSuggestionsAsync("test", 20))
                             .ReturnsAsync(expectedSuggestions);

        
        var result = await _controller.GetSuggestionsAsync("test", limit: 100);

                _mockSearchRepository.Verify(x => x.GetSearchSuggestionsAsync("test", 20), Times.Once);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WithValidLimit_ShouldUseProvidedLimit()
    {
        
        var expectedSuggestions = new[] { "suggestion" };
        _mockSearchRepository.Setup(x => x.GetSearchSuggestionsAsync("test", 5))
                             .ReturnsAsync(expectedSuggestions);

        
        var result = await _controller.GetSuggestionsAsync("test", limit: 5);

                _mockSearchRepository.Verify(x => x.GetSearchSuggestionsAsync("test", 5), Times.Once);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WhenRepositoryThrows_ShouldReturn500()
    {
        
        _mockSearchRepository.Setup(x => x.GetSearchSuggestionsAsync(It.IsAny<string>(), It.IsAny<int>()))
                             .ThrowsAsync(new Exception("Database error"));

        
        var result = await _controller.GetSuggestionsAsync("test");

                var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().BeEquivalentTo(new { error = "An error occurred while getting suggestions" });
    }

    #endregion

    #region RebuildIndexAsync Tests

    [Fact]
    public async Task RebuildIndexAsync_WhenSuccessful_ShouldReturnOkResult()
    {
        
        _mockSearchRepository.Setup(x => x.RebuildIndexAsync())
                             .Returns(Task.CompletedTask);

        
        var result = await _controller.RebuildIndexAsync();

                var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = "Search index has been rebuilt successfully" });
        _mockSearchRepository.Verify(x => x.RebuildIndexAsync(), Times.Once);
    }

    [Fact]
    public async Task RebuildIndexAsync_WhenRepositoryThrows_ShouldReturn500()
    {
        
        _mockSearchRepository.Setup(x => x.RebuildIndexAsync())
                             .ThrowsAsync(new Exception("Index rebuild failed"));

        
        var result = await _controller.RebuildIndexAsync();

                var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().BeEquivalentTo(new { error = "An error occurred while rebuilding the search index" });
    }

    #endregion

    #region Integration Parameter Tests

    [Theory]
    [InlineData(-1, 0, 1, 20)] // Both invalid
    [InlineData(0, -5, 1, 20)] // Both invalid
    [InlineData(5, 50, 5, 50)] // Both valid
    [InlineData(10, 1000, 10, 1000)] // PageSize is allowed by controller
    public async Task SearchAsync_ParameterValidation_ShouldNormalizeCorrectly(
        int inputPage, int inputPageSize, int expectedPage, int expectedPageSize)
    {
        
        var expectedResult = new PagedResult<Story> { Items = Array.Empty<Story>(), TotalCount = 0 };
        _mockSearchRepository.Setup(x => x.SearchStoriesAsync(It.IsAny<SearchQuery>()))
                             .ReturnsAsync(expectedResult);

        
        await _controller.SearchAsync("test", page: inputPage, pageSize: inputPageSize);

                _mockSearchRepository.Verify(x => x.SearchStoriesAsync(It.Is<SearchQuery>(q => 
            q.Page == expectedPage && q.PageSize == expectedPageSize)), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithNullFilters_ShouldCreateFiltersObject()
    {
        
        var expectedResult = new PagedResult<Story> { Items = Array.Empty<Story>(), TotalCount = 0 };
        _mockSearchRepository.Setup(x => x.SearchStoriesAsync(It.IsAny<SearchQuery>()))
                             .ReturnsAsync(expectedResult);

        
        await _controller.SearchAsync("test");

                _mockSearchRepository.Verify(x => x.SearchStoriesAsync(It.Is<SearchQuery>(q => 
            q.Filters != null)), Times.Once);
    }

    #endregion
}