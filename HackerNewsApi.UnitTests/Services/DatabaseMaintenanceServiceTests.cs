using FluentAssertions;
using HackerNewsApi.Infrastructure.Data;
using HackerNewsApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsApi.UnitTests.Services;

public class DatabaseMaintenanceServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly SearchDbContext _dbContext;
    private readonly Mock<ILogger<DatabaseMaintenanceService>> _mockLogger;
    private readonly DatabaseMaintenanceService _service;
    private readonly string _testDbPath;

    public DatabaseMaintenanceServiceTests()
    {
        // Create a temporary SQLite database file
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_maintenance_db_{Guid.NewGuid()}.db");
        
        var services = new ServiceCollection();
        
        // Configure SQLite database with temporary file
        services.AddDbContext<SearchDbContext>(options =>
            options.UseSqlite($"Data Source={_testDbPath}"));
        
        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<SearchDbContext>();
        
        // Ensure database is created
        _dbContext.Database.EnsureCreated();
        
        _mockLogger = new Mock<ILogger<DatabaseMaintenanceService>>();
        _service = new DatabaseMaintenanceService(_serviceProvider, _mockLogger.Object);
    }

    [Fact]
    public async Task GetCurrentStatsAsync_WithEmptyDatabase_ShouldReturnZeroStats()
    {
        // Act
        var stats = await _service.GetCurrentStatsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.StoryCount.Should().Be(0);
        stats.SizeInBytes.Should().BeGreaterThanOrEqualTo(0);
        stats.UsagePercentage.Should().BeGreaterThanOrEqualTo(0);
        stats.ShouldCleanupOldData.Should().BeFalse();
        stats.IsNearLimit.Should().BeFalse();
        stats.IsOverLimit.Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentStatsAsync_WithTestData_ShouldCalculateCorrectStats()
    {
        
        var oldStory = new StoryEntity
        {
            Id = 1,
            Title = "Old Story",
            Author = "author1",
            Score = 100,
            CreatedAt = DateTime.UtcNow.AddDays(-45),
            IndexedAt = DateTime.UtcNow.AddDays(-45)
        };

        var newStory = new StoryEntity
        {
            Id = 2,
            Title = "New Story",
            Author = "author2",
            Score = 150,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            IndexedAt = DateTime.UtcNow.AddDays(-5)
        };

        _dbContext.Stories.Add(oldStory);
        _dbContext.Stories.Add(newStory);
        await _dbContext.SaveChangesAsync();

        // Act
        var stats = await _service.GetCurrentStatsAsync();

        // Assert
        stats.StoryCount.Should().Be(2);
        stats.OldestStoryDate.Should().NotBeNull();
        stats.OldestStoryDate.Value.Should().BeCloseTo(oldStory.IndexedAt.Value, TimeSpan.FromSeconds(1));
        stats.NewestStoryDate.Should().NotBeNull();
        stats.NewestStoryDate.Value.Should().BeCloseTo(newStory.IndexedAt.Value, TimeSpan.FromSeconds(1));
        stats.ShouldCleanupOldData.Should().BeTrue(); // Has data older than 30 days
    }

    [Fact]
    public async Task ForceCleanupAsync_WithOldData_ShouldRemoveOldStories()
    {
        
        var oldStories = CreateTestStories(5, DateTime.UtcNow.AddDays(-45));
        var newStories = CreateTestStories(3, DateTime.UtcNow.AddDays(-10));
        
        _dbContext.Stories.AddRange(oldStories);
        _dbContext.Stories.AddRange(newStories);
        await _dbContext.SaveChangesAsync();

        var initialCount = await _dbContext.Stories.CountAsync();
        initialCount.Should().Be(8);

        // Act
        await _service.ForceCleanupAsync();

        // Assert
        var remainingCount = await _dbContext.Stories.CountAsync();
        remainingCount.Should().Be(3); // Only new stories should remain
        
        var remainingStories = await _dbContext.Stories.ToListAsync();
        remainingStories.Should().AllSatisfy(s =>
        {
            s.IndexedAt.Should().BeAfter(DateTime.UtcNow.AddDays(-30));
        });
    }

    [Fact]
    public async Task ForceCleanupAsync_WithNoOldData_ShouldNotRemoveAnyStories()
    {
        
        var recentStories = CreateTestStories(5, DateTime.UtcNow.AddDays(-10));
        
        _dbContext.Stories.AddRange(recentStories);
        await _dbContext.SaveChangesAsync();

        var initialCount = await _dbContext.Stories.CountAsync();

        // Act
        await _service.ForceCleanupAsync();

        // Assert
        var finalCount = await _dbContext.Stories.CountAsync();
        finalCount.Should().Be(initialCount); // No stories should be removed
    }

    [Fact]
    public void DatabaseStats_Properties_ShouldCalculateCorrectly()
    {
        var stats = new DatabaseStats
        {
            SizeInBytes = 850_000_000, // 850MB
            UsagePercentage = 85
        };

        // Assert
        stats.IsNearLimit.Should().BeTrue(); // > 80%
        stats.IsOverLimit.Should().BeFalse(); // < 100%

        // Test over limit
        stats.UsagePercentage = 105;
        stats.IsOverLimit.Should().BeTrue();
    }

    [Fact]
    public void DatabaseMaintenanceService_AsHostedService_ShouldImplementIHostedService()
    {
        // Assert
        _service.Should().BeAssignableTo<IHostedService>();
        _service.Should().BeAssignableTo<BackgroundService>();
    }

    [Fact]
    public async Task GetCurrentStatsAsync_ShouldCalculateUsagePercentage()
    {
        
        var stories = CreateTestStories(100, DateTime.UtcNow.AddDays(-10));
        _dbContext.Stories.AddRange(stories);
        await _dbContext.SaveChangesAsync();

        // Act
        var stats = await _service.GetCurrentStatsAsync();

        // Assert
        stats.UsagePercentage.Should().BeGreaterThanOrEqualTo(0);
        stats.SizeInMB.Should().BeGreaterThanOrEqualTo(0);
        stats.SizeInBytes.Should().BeGreaterThanOrEqualTo(0);
    }

    [Theory]
    [InlineData(10, false)]  // 10 days old - should not cleanup
    [InlineData(35, true)]   // 35 days old - should cleanup
    [InlineData(50, true)]   // 50 days old - should cleanup
    public async Task GetCurrentStatsAsync_ShouldCorrectlyIdentifyOldData(int daysOld, bool expectedCleanup)
    {
        
        var story = new StoryEntity
        {
            Id = 1,
            Title = "Test Story",
            Author = "author",
            Score = 100,
            CreatedAt = DateTime.UtcNow.AddDays(-daysOld),
            IndexedAt = DateTime.UtcNow.AddDays(-daysOld)
        };

        _dbContext.Stories.Add(story);
        await _dbContext.SaveChangesAsync();

        // Act
        var stats = await _service.GetCurrentStatsAsync();

        // Assert
        stats.ShouldCleanupOldData.Should().Be(expectedCleanup);
    }

    [Fact]
    public async Task ForceCleanupAsync_WithLargeDataset_ShouldProcessInBatches()
    {
        var oldStories = CreateTestStories(150, DateTime.UtcNow.AddDays(-45));
        var newStories = CreateTestStories(10, DateTime.UtcNow.AddDays(-10));
        
        _dbContext.Stories.AddRange(oldStories);
        _dbContext.Stories.AddRange(newStories);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.ForceCleanupAsync();

        // Assert
        var remainingCount = await _dbContext.Stories.CountAsync();
        remainingCount.Should().Be(10); // Only new stories should remain
        
        // Verify cleanup was logged
        VerifyLoggerWasCalled(LogLevel.Information, Times.AtLeastOnce());
    }

    private List<StoryEntity> CreateTestStories(int count, DateTime indexedAt)
    {
        var stories = new List<StoryEntity>();
        for (int i = 0; i < count; i++)
        {
            stories.Add(new StoryEntity
            {
                Id = GetNextId(),
                Title = $"Test Story {i}",
                Author = $"author{i}",
                Score = 100 + i,
                CreatedAt = indexedAt,
                IndexedAt = indexedAt,
                Url = $"https://example.com/story{i}"
            });
        }
        return stories;
    }

    private static int _nextId = 1;
    private static int GetNextId() => _nextId++;

    private void VerifyLoggerWasCalled(LogLevel level, Times? times = null)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            times ?? Times.AtLeastOnce());
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
        
        // Clean up the test database file
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}