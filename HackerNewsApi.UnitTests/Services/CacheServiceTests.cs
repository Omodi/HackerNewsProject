using FluentAssertions;
using HackerNewsApi.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace HackerNewsApi.UnitTests.Services;

public class CacheServiceTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheService _cacheService;

    public CacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cacheService = new CacheService(_memoryCache);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnValue()
    {
        // Arrange
        var key = "test_key";
        var value = new TestObject { Id = 1, Name = "Test" };
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(10));

        // Act
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(value.Id);
        result.Name.Should().Be(value.Name);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnDefault()
    {
        // Arrange
        var key = "nonexistent_key";

        // Act
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldStoreValueInCache()
    {
        // Arrange
        var key = "set_test_key";
        var value = new TestObject { Id = 42, Name = "SetTest" };

        // Act
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("SetTest");
    }

    [Fact]
    public async Task SetAsync_WithPrimitiveType_ShouldStoreAndRetrieve()
    {
        // Arrange
        var key = "primitive_key";
        var value = 12345;

        // Act
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var result = await _cacheService.GetAsync<int>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithArray_ShouldStoreAndRetrieve()
    {
        // Arrange
        var key = "array_key";
        var value = new[] { 1, 2, 3, 4, 5 };

        // Act
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var result = await _cacheService.GetAsync<int[]>(key);

        // Assert
        result.Should().NotBeNull();
        result.Should().Equal(value);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveValueFromCache()
    {
        // Arrange
        var key = "remove_test_key";
        var value = new TestObject { Id = 99, Name = "ToRemove" };
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(10));

        // Verify it exists first
        var beforeRemove = await _cacheService.GetAsync<TestObject>(key);
        beforeRemove.Should().NotBeNull();

        // Act
        await _cacheService.RemoveAsync(key);
        var afterRemove = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        afterRemove.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WithNonExistentKey_ShouldNotThrow()
    {
        // Arrange
        var key = "nonexistent_remove_key";

        // Act & Assert (should not throw)
        var act = async () => await _cacheService.RemoveAsync(key);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAsync_WithExpiredEntry_ShouldReturnDefault()
    {
        // Arrange
        var key = "expired_key";
        var value = new TestObject { Id = 100, Name = "Expired" };
        
        // Set with very short expiry
        await _cacheService.SetAsync(key, value, TimeSpan.FromMilliseconds(1));
        
        // Wait for expiry
        await Task.Delay(50);

        // Act
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_OverwriteExistingKey_ShouldUpdateValue()
    {
        // Arrange
        var key = "overwrite_key";
        var originalValue = new TestObject { Id = 1, Name = "Original" };
        var newValue = new TestObject { Id = 2, Name = "Updated" };

        // Act
        await _cacheService.SetAsync(key, originalValue, TimeSpan.FromMinutes(10));
        await _cacheService.SetAsync(key, newValue, TimeSpan.FromMinutes(10));
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.Name.Should().Be("Updated");
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}