using FluentAssertions;
using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using HackerNewsApi.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsApi.UnitTests.Services;

public class HackerNewsServiceTests
{
    private readonly Mock<IHackerNewsApiClient> _mockApiClient;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<HackerNewsService>> _mockLogger;
    private readonly HackerNewsService _service;

    public HackerNewsServiceTests()
    {
        _mockApiClient = new Mock<IHackerNewsApiClient>();
        _mockCacheService = new Mock<ICacheService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<HackerNewsService>>();
        _service = new HackerNewsService(_mockApiClient.Object, _mockCacheService.Object, _mockServiceProvider.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetNewStoryIdsAsync_WhenCacheHit_ShouldReturnCachedIds()
    {
        
        var cachedIds = new[] { 1, 2, 3, 4, 5 };
        _mockCacheService.Setup(x => x.GetAsync<int[]>("story_ids"))
                         .ReturnsAsync(cachedIds);

        
        var result = await _service.GetNewStoryIdsAsync();

        
        result.Should().Equal(cachedIds);
        _mockApiClient.Verify(x => x.GetNewStoryIdsAsync(), Times.Never);
    }

    [Fact]
    public async Task GetNewStoryIdsAsync_WhenCacheMiss_ShouldCallApiAndCache()
    {
        
        var apiIds = new[] { 10, 20, 30 };
        _mockCacheService.Setup(x => x.GetAsync<int[]>("story_ids"))
                         .ReturnsAsync((int[]?)null);
        _mockApiClient.Setup(x => x.GetNewStoryIdsAsync())
                      .ReturnsAsync(apiIds);

        
        var result = await _service.GetNewStoryIdsAsync();

        
        result.Should().Equal(apiIds);
        _mockCacheService.Verify(x => x.SetAsync("story_ids", apiIds, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetStoryAsync_WhenCacheHit_ShouldReturnCachedStory()
    {
        
        var storyId = 123;
        var cachedStory = new Story { Id = storyId, Title = "Test Story" };
        _mockCacheService.Setup(x => x.GetAsync<Story>($"story_{storyId}"))
                         .ReturnsAsync(cachedStory);

        
        var result = await _service.GetStoryAsync(storyId);

        
        result.Should().BeEquivalentTo(cachedStory);
        _mockApiClient.Verify(x => x.GetStoryAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetStoryAsync_WhenCacheMiss_ShouldCallApiAndCache()
    {
        
        var storyId = 456;
        var apiStory = new Story { Id = storyId, Title = "API Story" };
        _mockCacheService.Setup(x => x.GetAsync<Story>($"story_{storyId}"))
                         .ReturnsAsync((Story?)null);
        _mockApiClient.Setup(x => x.GetStoryAsync(storyId))
                      .ReturnsAsync(apiStory);

        
        var result = await _service.GetStoryAsync(storyId);

        
        result.Should().BeEquivalentTo(apiStory);
        _mockCacheService.Verify(x => x.SetAsync($"story_{storyId}", apiStory, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetStoryAsync_WhenApiReturnsNull_ShouldNotCache()
    {
        
        var storyId = 789;
        _mockCacheService.Setup(x => x.GetAsync<Story>($"story_{storyId}"))
                         .ReturnsAsync((Story?)null);
        _mockApiClient.Setup(x => x.GetStoryAsync(storyId))
                      .ReturnsAsync((Story?)null);

        
        var result = await _service.GetStoryAsync(storyId);

        
        result.Should().BeNull();
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Story>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetStoriesAsync_WhenCacheMiss_ShouldReturnPagedResultFromApi()
    {
        
        var storyIds = new[] { 1, 2, 3, 4, 5 };
        var stories = new[]
        {
            new Story { Id = 1, Title = "Story 1" },
            new Story { Id = 2, Title = "Story 2" }
        };

        // Mock cache miss for stories page
        _mockCacheService.Setup(x => x.GetAsync<PagedResult<Story>>("stories_page_1_2"))
                         .ReturnsAsync((PagedResult<Story>?)null);

        // Mock story IDs and individual stories
        _mockCacheService.Setup(x => x.GetAsync<int[]>("story_ids"))
                         .ReturnsAsync(storyIds);
        _mockCacheService.Setup(x => x.GetAsync<Story>("story_1"))
                         .ReturnsAsync(stories[0]);
        _mockCacheService.Setup(x => x.GetAsync<Story>("story_2"))
                         .ReturnsAsync(stories[1]);

        
        var result = await _service.GetStoriesAsync(1, 2);

        
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().BeEquivalentTo(stories);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(5);

        // Verify the page result was cached
        _mockCacheService.Verify(x => x.SetAsync("stories_page_1_2", It.IsAny<PagedResult<Story>>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetStoriesAsync_WhenCacheHit_ShouldReturnCachedPagedResult()
    {
        
        var cachedResult = new PagedResult<Story>
        {
            Items = new[]
            {
                new Story { Id = 1, Title = "Cached Story 1" },
                new Story { Id = 2, Title = "Cached Story 2" }
            },
            Page = 1,
            PageSize = 2,
            TotalCount = 5
        };

        _mockCacheService.Setup(x => x.GetAsync<PagedResult<Story>>("stories_page_1_2"))
                         .ReturnsAsync(cachedResult);

        
        var result = await _service.GetStoriesAsync(1, 2);

        
        result.Should().BeEquivalentTo(cachedResult);
        
        // Verify no API calls were made
        _mockApiClient.Verify(x => x.GetNewStoryIdsAsync(), Times.Never);
        _mockApiClient.Verify(x => x.GetStoryAsync(It.IsAny<int>()), Times.Never);
    }

}