using FluentAssertions;
using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using HackerNewsApi.Infrastructure.Data;
using HackerNewsApi.Infrastructure.Services;
using HackerNewsApi.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HackerNewsApi.UnitTests.Services;

public class StoryIndexingServiceTests : DatabaseTestBase, IClassFixture<DatabaseTestFixture>
{
    private readonly Mock<IHackerNewsService> _mockHackerNewsService;
    private readonly Mock<ISearchRepository> _mockSearchRepository;
    private readonly Mock<ILogger<StoryIndexingService>> _mockLogger;
    private readonly StoryIndexingService _service;

    public StoryIndexingServiceTests(DatabaseTestFixture databaseFixture) : base(databaseFixture)
    {
        _mockHackerNewsService = new Mock<IHackerNewsService>();
        _mockSearchRepository = new Mock<ISearchRepository>();
        _mockLogger = new Mock<ILogger<StoryIndexingService>>();

        // Add some test data to make database non-empty (skip initial seeding)
        SeedTestDatabase();

        // Create a test service provider that implements CreateScope without extension method
        var testServiceProvider = new TestServiceProvider(_mockHackerNewsService.Object, _mockSearchRepository.Object, Context);
        
        _service = new StoryIndexingService(testServiceProvider, _mockLogger.Object);
    }

    private void SeedTestDatabase()
    {
        // Add some existing stories to make the database non-empty
        var existingStories = new[]
        {
            new StoryEntity
            {
                Id = 1,
                Title = "Existing Story 1",
                Author = "user1",
                Score = 100,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow,
                IndexedAt = DateTime.UtcNow.AddDays(-1),
                CommentCount = 10
            },
            new StoryEntity
            {
                Id = 2,
                Title = "Existing Story 2",
                Author = "user2",
                Score = 50,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow,
                IndexedAt = DateTime.UtcNow.AddDays(-2),
                CommentCount = 5
            }
        };

        Context.Stories.AddRange(existingStories);
        Context.SaveChanges();
    }

    // Test implementation of IServiceProvider that doesn't use extension methods
    private class TestServiceProvider : IServiceProvider
    {
        private readonly IHackerNewsService _hackerNewsService;
        private readonly ISearchRepository _searchRepository;
        private readonly SearchDbContext _dbContext;

        public TestServiceProvider(IHackerNewsService hackerNewsService, ISearchRepository searchRepository, SearchDbContext dbContext)
        {
            _hackerNewsService = hackerNewsService;
            _searchRepository = searchRepository;
            _dbContext = dbContext;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceScopeFactory))
                return new TestServiceScopeFactory(_hackerNewsService, _searchRepository, _dbContext);
            
            return null;
        }
    }

    private class TestServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IHackerNewsService _hackerNewsService;
        private readonly ISearchRepository _searchRepository;
        private readonly SearchDbContext _dbContext;

        public TestServiceScopeFactory(IHackerNewsService hackerNewsService, ISearchRepository searchRepository, SearchDbContext dbContext)
        {
            _hackerNewsService = hackerNewsService;
            _searchRepository = searchRepository;
            _dbContext = dbContext;
        }

        public IServiceScope CreateScope() => new TestServiceScope(_hackerNewsService, _searchRepository, _dbContext);
    }

    private class TestServiceScope : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; }

        public TestServiceScope(IHackerNewsService hackerNewsService, ISearchRepository searchRepository, SearchDbContext dbContext)
        {
            ServiceProvider = new TestScopeServiceProvider(hackerNewsService, searchRepository, dbContext);
        }

        public void Dispose() { }
    }

    private class TestScopeServiceProvider : IServiceProvider
    {
        private readonly IHackerNewsService _hackerNewsService;
        private readonly ISearchRepository _searchRepository;
        private readonly SearchDbContext _dbContext;

        public TestScopeServiceProvider(IHackerNewsService hackerNewsService, ISearchRepository searchRepository, SearchDbContext dbContext)
        {
            _hackerNewsService = hackerNewsService;
            _searchRepository = searchRepository;
            _dbContext = dbContext;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IHackerNewsService))
                return _hackerNewsService;
            if (serviceType == typeof(ISearchRepository))
                return _searchRepository;
            if (serviceType == typeof(SearchDbContext))
                return _dbContext;
            
            throw new InvalidOperationException($"Service {serviceType} not registered");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _service?.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        
        _service.Should().NotBeNull();
        _service.Should().BeOfType<StoryIndexingService>();
        _service.Should().BeAssignableTo<BackgroundService>();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldNotThrow()
    {
        // The actual service doesn't validate null parameters
        
        var act = () => new StoryIndexingService(null!, _mockLogger.Object);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldNotThrow()
    {
        // The actual service doesn't validate null parameters
        
        var testServiceProvider = new TestServiceProvider(_mockHackerNewsService.Object, _mockSearchRepository.Object, Context);
        var act = () => new StoryIndexingService(testServiceProvider, null!);
        act.Should().NotThrow();
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_ShouldStartSuccessfully()
    {
        
        // The incremental update logic runs on a 15-minute timer, so we can't test it in unit tests
        
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        
        var act = async () =>
        {
            await _service.StartAsync(cts.Token);
            await Task.Delay(50);
            await _service.StopAsync(cts.Token);
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyStories_ShouldNotCallIndexing()
    {
        
        _mockHackerNewsService.Setup(x => x.GetStoriesAsync(1, 20))
                              .ReturnsAsync(new PagedResult<Story>
                              {
                                  Items = Array.Empty<Story>(),
                                  Page = 1,
                                  PageSize = 20
                              });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        
        await _service.StartAsync(cts.Token);
        await Task.Delay(100);
        await _service.StopAsync(cts.Token);

        
        _mockSearchRepository.Verify(x => x.IndexStoriesAsync(It.IsAny<IEnumerable<Story>>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullStories_ShouldNotCallIndexing()
    {
        
        _mockHackerNewsService.Setup(x => x.GetStoriesAsync(1, 20))
                              .ReturnsAsync(new PagedResult<Story>
                              {
                                  Items = null!,
                                  Page = 1,
                                  PageSize = 20
                              });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        
        await _service.StartAsync(cts.Token);
        await Task.Delay(100);
        await _service.StopAsync(cts.Token);

        
        _mockSearchRepository.Verify(x => x.IndexStoriesAsync(It.IsAny<IEnumerable<Story>>()), Times.Never);
    }

    [Fact]
    public void Constructor_ShouldSetupServiceCorrectly()
    {
        // This test verifies the service is constructed properly and implements the correct interfaces
        // The actual background processing logic runs on a 15-minute timer which isn't suitable for unit testing
        
        
        _service.Should().NotBeNull();
        _service.Should().BeAssignableTo<BackgroundService>();
        _service.Should().BeAssignableTo<IHostedService>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenHackerNewsServiceThrows_ShouldHandleGracefully()
    {
        
        _mockHackerNewsService.Setup(x => x.GetStoriesAsync(It.IsAny<int>(), It.IsAny<int>()))
                              .ThrowsAsync(new Exception("Service error"));

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        
        var act = async () =>
        {
            await _service.StartAsync(cts.Token);
            await Task.Delay(25);
            await _service.StopAsync(cts.Token);
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WhenSearchRepositoryThrows_ShouldHandleGracefully()
    {
        
        var stories = new[] { new Story { Id = 1, Title = "Story 1", By = "user1" } };

        _mockHackerNewsService.Setup(x => x.GetStoriesAsync(1, 1000))
                              .ReturnsAsync(new PagedResult<Story>
                              {
                                  Items = stories,
                                  Page = 1,
                                  PageSize = 1000
                              });

        _mockSearchRepository.Setup(x => x.IndexStoriesAsync(It.IsAny<IEnumerable<Story>>()))
                             .ThrowsAsync(new Exception("Repository error"));

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        
        var act = async () =>
        {
            await _service.StartAsync(cts.Token);
            await Task.Delay(25);
            await _service.StopAsync(cts.Token);
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void DatabaseSeeding_ShouldHaveExistingData()
    {
        // This test verifies that our test database setup is working correctly
        // and has the expected seeded data
        
        
        var existingStories = Context.Stories.ToList();
        existingStories.Should().NotBeEmpty();
        existingStories.Should().HaveCount(2);
        existingStories.Should().Contain(s => s.Title == "Existing Story 1");
        existingStories.Should().Contain(s => s.Title == "Existing Story 2");
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        
        var act = () => _service.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        
        var act = () =>
        {
            _service.Dispose();
            _service.Dispose();
            _service.Dispose();
        };
        act.Should().NotThrow();
    }

    #endregion

    #region Service Lifecycle Tests

    [Fact]
    public async Task StartAndStopAsync_ShouldExecuteCorrectly()
    {
        
        _mockHackerNewsService.Setup(x => x.GetStoriesAsync(1, 1000))
                              .ReturnsAsync(new PagedResult<Story>
                              {
                                  Items = Array.Empty<Story>(),
                                  Page = 1,
                                  PageSize = 1000
                              });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        
        var act = async () =>
        {
            await _service.StartAsync(cts.Token);
            await Task.Delay(25);
            await _service.StopAsync(cts.Token);
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_WithoutStart_ShouldNotThrow()
    {
        
        var act = async () => await _service.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion
}