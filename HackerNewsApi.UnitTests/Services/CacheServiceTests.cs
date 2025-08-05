using FluentAssertions;
using HackerNewsApi.Infrastructure.Data;
using HackerNewsApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsApi.UnitTests.Services;

public class CacheServiceTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<SearchDbContext> _mockDbContext;
    private readonly Mock<ILogger<CacheService>> _mockLogger;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly CacheService _cacheService;

    public CacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        
        // Create mock SearchDbContext
        var options = new DbContextOptionsBuilder<SearchDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _mockDbContext = new Mock<SearchDbContext>(options);
        
        _mockLogger = new Mock<ILogger<CacheService>>();
        
        // Setup mocks for dependency injection
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        
        // Configure the mocks
        _mockServiceProvider.Setup(x => x.GetService(typeof(SearchDbContext)))
            .Returns(_mockDbContext.Object);
        _mockServiceScope.Setup(x => x.ServiceProvider)
            .Returns(_mockServiceProvider.Object);
        _mockServiceScopeFactory.Setup(x => x.CreateScope())
            .Returns(_mockServiceScope.Object);
        
        _cacheService = new CacheService(_memoryCache, _mockServiceScopeFactory.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnValue()
    {
        
        var key = "test_key";
        var value = new TestObject { Id = 1, Name = "Test" };
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(10));

        
        var result = await _cacheService.GetAsync<TestObject>(key);

        
        result.Should().NotBeNull();
        result!.Id.Should().Be(value.Id);
        result.Name.Should().Be(value.Name);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnDefault()
    {
        
        var key = "nonexistent_key";

        
        var result = await _cacheService.GetAsync<TestObject>(key);

        
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldStoreValueInCache()
    {
        
        var key = "set_test_key";
        var value = new TestObject { Id = 42, Name = "SetTest" };

        
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var result = await _cacheService.GetAsync<TestObject>(key);

        
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("SetTest");
    }

    [Fact]
    public async Task SetAsync_WithPrimitiveType_ShouldStoreAndRetrieve()
    {
        
        var key = "primitive_key";
        var value = 12345;

        
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var result = await _cacheService.GetAsync<int>(key);

        
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithArray_ShouldStoreAndRetrieve()
    {
        
        var key = "array_key";
        var value = new[] { 1, 2, 3, 4, 5 };

        
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var result = await _cacheService.GetAsync<int[]>(key);

        
        result.Should().NotBeNull();
        result.Should().Equal(value);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveValueFromCache()
    {
        
        var key = "remove_test_key";
        var value = new TestObject { Id = 99, Name = "ToRemove" };
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(10));

        // Verify it exists first
        var beforeRemove = await _cacheService.GetAsync<TestObject>(key);
        beforeRemove.Should().NotBeNull();

        
        await _cacheService.RemoveAsync(key);
        var afterRemove = await _cacheService.GetAsync<TestObject>(key);

        
        afterRemove.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WithNonExistentKey_ShouldNotThrow()
    {
        
        var key = "nonexistent_remove_key";

        var act = async () => await _cacheService.RemoveAsync(key);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAsync_WithExpiredEntry_ShouldReturnDefault()
    {
        
        var key = "expired_key";
        var value = new TestObject { Id = 100, Name = "Expired" };
        
        // Set with very short expiry
        await _cacheService.SetAsync(key, value, TimeSpan.FromMilliseconds(1));
        
        // Wait for expiry
        await Task.Delay(50);

        
        var result = await _cacheService.GetAsync<TestObject>(key);

        
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_OverwriteExistingKey_ShouldUpdateValue()
    {
        
        var key = "overwrite_key";
        var originalValue = new TestObject { Id = 1, Name = "Original" };
        var newValue = new TestObject { Id = 2, Name = "Updated" };

        
        await _cacheService.SetAsync(key, originalValue, TimeSpan.FromMinutes(10));
        await _cacheService.SetAsync(key, newValue, TimeSpan.FromMinutes(10));
        var result = await _cacheService.GetAsync<TestObject>(key);

        
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.Name.Should().Be("Updated");
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
        _mockDbContext?.Object?.Dispose();
        _mockServiceScope?.Object?.Dispose();
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}