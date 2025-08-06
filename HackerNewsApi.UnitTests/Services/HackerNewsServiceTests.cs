using FluentAssertions;
using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using HackerNewsApi.Infrastructure.Data;
using HackerNewsApi.Infrastructure.Services;
using HackerNewsApi.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsApi.UnitTests.Services;

public class HackerNewsServiceTests : DatabaseTestBase
{
    private readonly Mock<IHackerNewsApiClient> _mockApiClient;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<HackerNewsService>> _mockLogger;
    private readonly HackerNewsService _service;

    public HackerNewsServiceTests(DatabaseTestFixture databaseFixture) : base(databaseFixture)
    {
        _mockApiClient = new Mock<IHackerNewsApiClient>();
        _mockCacheService = new Mock<ICacheService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<HackerNewsService>>();
        _service = new HackerNewsService(_mockApiClient.Object, _mockCacheService.Object, Context, _mockServiceProvider.Object, _mockLogger.Object);
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
    public async Task GetStoryAsync_WhenCacheAndDbMiss_ShouldCallApiAndCache()
    {
        // Arrange
        await CleanDatabaseAsync(); // Ensure clean state
        
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
    public async Task GetStoryAsync_WhenCacheDbAndApiMiss_ShouldReturnNull()
    {
        // Arrange
        await CleanDatabaseAsync(); // Ensure clean state
        
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
    public async Task GetStoryAsync_WhenDbHit_ShouldReturnStoryFromDbAndCache()
    {
        // Arrange
        await CleanDatabaseAsync(); // Ensure clean state
        
        var storyId = 999;
        var dbStory = new StoryEntity
        {
            Id = storyId,
            Title = "DB Story",
            Author = "DB Author",
            Url = "https://example.com",
            Score = 100,
            CreatedAt = DateTime.UtcNow,
            CommentCount = 5
        };
        
        // Add story to database
        Context.Stories.Add(dbStory);
        await Context.SaveChangesAsync();
        
        _mockCacheService.Setup(x => x.GetAsync<Story>($"story_{storyId}"))
                         .ReturnsAsync((Story?)null);

        // Act
        var result = await _service.GetStoryAsync(storyId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(storyId);
        result.Title.Should().Be("DB Story");
        result.By.Should().Be("DB Author");
        
        // Verify that story was cached after retrieving from DB
        _mockCacheService.Verify(x => x.SetAsync($"story_{storyId}", It.IsAny<Story>(), It.IsAny<TimeSpan>()), Times.Once);
        
        // Verify API was not called
        _mockApiClient.Verify(x => x.GetStoryAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetStoriesAsync_ShouldReturnPagedResultFromApi()
    {
        // Arrange
        await CleanDatabaseAsync(); // Ensure clean state
        
        var storyIds = new[] { 1, 2, 3, 4, 5 };
        var stories = new[]
        {
            new Story { Id = 1, Title = "Story 1" },
            new Story { Id = 2, Title = "Story 2" }
        };

        // Mock story IDs and individual stories (no page caching anymore)
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
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);

        // Verify no page caching happened (pages are no longer cached)
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.Is<PagedResult<Story>>(p => true), It.IsAny<TimeSpan>()), Times.Never);
    }

}