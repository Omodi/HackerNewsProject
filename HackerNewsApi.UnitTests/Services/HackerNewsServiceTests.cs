using FluentAssertions;
using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using HackerNewsApi.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsApi.UnitTests.Services;

public class HackerNewsServiceTests
{
    private readonly Mock<IHackerNewsApiClient> _mockApiClient;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<HackerNewsService>> _mockLogger;
    private readonly HackerNewsService _service;

    public HackerNewsServiceTests()
    {
        _mockApiClient = new Mock<IHackerNewsApiClient>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<HackerNewsService>>();
        _service = new HackerNewsService(_mockApiClient.Object, _mockCacheService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetNewStoryIdsAsync_WhenCacheHit_ShouldReturnCachedIds()
    {
        // Arrange
        var cachedIds = new[] { 1, 2, 3, 4, 5 };
        _mockCacheService.Setup(x => x.GetAsync<int[]>("story_ids"))
                         .ReturnsAsync(cachedIds);

        // Act
        var result = await _service.GetNewStoryIdsAsync();

        // Assert
        result.Should().Equal(cachedIds);
        _mockApiClient.Verify(x => x.GetNewStoryIdsAsync(), Times.Never);
    }

    [Fact]
    public async Task GetNewStoryIdsAsync_WhenCacheMiss_ShouldCallApiAndCache()
    {
        // Arrange
        var apiIds = new[] { 10, 20, 30 };
        _mockCacheService.Setup(x => x.GetAsync<int[]>("story_ids"))
                         .ReturnsAsync((int[]?)null);
        _mockApiClient.Setup(x => x.GetNewStoryIdsAsync())
                      .ReturnsAsync(apiIds);

        // Act
        var result = await _service.GetNewStoryIdsAsync();

        // Assert
        result.Should().Equal(apiIds);
        _mockCacheService.Verify(x => x.SetAsync("story_ids", apiIds, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetStoryAsync_WhenCacheHit_ShouldReturnCachedStory()
    {
        // Arrange
        var storyId = 123;
        var cachedStory = new Story { Id = storyId, Title = "Test Story" };
        _mockCacheService.Setup(x => x.GetAsync<Story>($"story_{storyId}"))
                         .ReturnsAsync(cachedStory);

        // Act
        var result = await _service.GetStoryAsync(storyId);

        // Assert
        result.Should().BeEquivalentTo(cachedStory);
        _mockApiClient.Verify(x => x.GetStoryAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetStoryAsync_WhenCacheMiss_ShouldCallApiAndCache()
    {
        // Arrange
        var storyId = 456;
        var apiStory = new Story { Id = storyId, Title = "API Story" };
        _mockCacheService.Setup(x => x.GetAsync<Story>($"story_{storyId}"))
                         .ReturnsAsync((Story?)null);
        _mockApiClient.Setup(x => x.GetStoryAsync(storyId))
                      .ReturnsAsync(apiStory);

        // Act
        var result = await _service.GetStoryAsync(storyId);

        // Assert
        result.Should().BeEquivalentTo(apiStory);
        _mockCacheService.Verify(x => x.SetAsync($"story_{storyId}", apiStory, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetStoryAsync_WhenApiReturnsNull_ShouldNotCache()
    {
        // Arrange
        var storyId = 789;
        _mockCacheService.Setup(x => x.GetAsync<Story>($"story_{storyId}"))
                         .ReturnsAsync((Story?)null);
        _mockApiClient.Setup(x => x.GetStoryAsync(storyId))
                      .ReturnsAsync((Story?)null);

        // Act
        var result = await _service.GetStoryAsync(storyId);

        // Assert
        result.Should().BeNull();
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Story>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetStoriesAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var storyIds = new[] { 1, 2, 3, 4, 5 };
        var stories = new[]
        {
            new Story { Id = 1, Title = "Story 1" },
            new Story { Id = 2, Title = "Story 2" }
        };

        _mockCacheService.Setup(x => x.GetAsync<int[]>("story_ids"))
                         .ReturnsAsync(storyIds);
        _mockCacheService.Setup(x => x.GetAsync<Story>("story_1"))
                         .ReturnsAsync(stories[0]);
        _mockCacheService.Setup(x => x.GetAsync<Story>("story_2"))
                         .ReturnsAsync(stories[1]);

        // Act
        var result = await _service.GetStoriesAsync(1, 2);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().BeEquivalentTo(stories);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task SearchStoriesAsync_WithEmptyQuery_ShouldReturnRegularStories()
    {
        // Arrange
        var storyIds = new[] { 1, 2 };
        var stories = new[]
        {
            new Story { Id = 1, Title = "Story 1" },
            new Story { Id = 2, Title = "Story 2" }
        };

        _mockCacheService.Setup(x => x.GetAsync<int[]>("story_ids"))
                         .ReturnsAsync(storyIds);
        _mockCacheService.Setup(x => x.GetAsync<Story>("story_1"))
                         .ReturnsAsync(stories[0]);
        _mockCacheService.Setup(x => x.GetAsync<Story>("story_2"))
                         .ReturnsAsync(stories[1]);

        // Act
        var result = await _service.SearchStoriesAsync("", 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchStoriesAsync_WithQuery_ShouldReturnFilteredResults()
    {
        // Arrange
        var query = "React";
        var storyIds = new[] { 1, 2, 3 };
        var stories = new[]
        {
            new Story { Id = 1, Title = "React Tutorial" },
            new Story { Id = 2, Title = "Vue.js Guide" },
            new Story { Id = 3, Title = "React Hooks" }
        };

        _mockCacheService.Setup(x => x.GetAsync<PagedResult<Story>>($"search_{query.ToLowerInvariant()}_1_20"))
                         .ReturnsAsync((PagedResult<Story>?)null);
        _mockCacheService.Setup(x => x.GetAsync<int[]>("story_ids"))
                         .ReturnsAsync(storyIds);
        _mockCacheService.Setup(x => x.GetAsync<Story>("story_1"))
                         .ReturnsAsync(stories[0]);
        _mockCacheService.Setup(x => x.GetAsync<Story>("story_2"))
                         .ReturnsAsync(stories[1]);
        _mockCacheService.Setup(x => x.GetAsync<Story>("story_3"))
                         .ReturnsAsync(stories[2]);

        // Act
        var result = await _service.SearchStoriesAsync(query, 1, 20);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(s => s.Title.Contains("React"));
        result.Items.Should().NotContain(s => s.Title.Contains("Vue"));
        result.TotalCount.Should().Be(2);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<PagedResult<Story>>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task SearchStoriesAsync_WithCachedResults_ShouldReturnCachedData()
    {
        // Arrange
        var query = "Test";
        var cachedResult = new PagedResult<Story>
        {
            Items = new[] { new Story { Id = 1, Title = "Test Story" } },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mockCacheService.Setup(x => x.GetAsync<PagedResult<Story>>($"search_{query.ToLowerInvariant()}_1_20"))
                         .ReturnsAsync(cachedResult);

        // Act
        var result = await _service.SearchStoriesAsync(query, 1, 20);

        // Assert
        result.Should().BeEquivalentTo(cachedResult);
        _mockCacheService.Verify(x => x.GetAsync<int[]>("story_ids"), Times.Never);
    }
}