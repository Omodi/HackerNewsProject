using FluentAssertions;
using HackerNewsApi.Core.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace HackerNewsApi.IntegrationTests.Controllers;

public class StoriesControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public StoriesControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_HealthEndpoint_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        content.Should().Contain("Healthy");
        content.Should().Contain("timestamp");
        content.Should().Contain("version");
    }

    [Fact]
    public async Task Get_Stories_ShouldReturnPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/stories?page=1&pageSize=5");

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(5);
        result.TotalCount.Should().BeGreaterThan(0);
        result.Items.Should().HaveCountLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task Get_Stories_WithInvalidPage_ShouldNormalizePage()
    {
        // Act
        var response = await _client.GetAsync("/api/stories?page=-1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

        result.Should().NotBeNull();
        result!.Page.Should().Be(1); // Should be normalized to 1
    }

    [Fact]
    public async Task Get_Stories_WithInvalidPageSize_ShouldNormalizePageSize()
    {
        // Act
        var response = await _client.GetAsync("/api/stories?page=1&pageSize=2000");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

        result.Should().NotBeNull();
        result!.PageSize.Should().Be(20); // Should be normalized to 20
    }

    [Fact]
    public async Task Get_Stories_SecondPage_ShouldReturnDifferentStories()
    {
        // Arrange
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var page1Response = await _client.GetAsync("/api/stories?page=1&pageSize=3");
        var page2Response = await _client.GetAsync("/api/stories?page=2&pageSize=3");

        // Assert
        page1Response.EnsureSuccessStatusCode();
        page2Response.EnsureSuccessStatusCode();

        var page1Result = await page1Response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);
        var page2Result = await page2Response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

        page1Result.Should().NotBeNull();
        page2Result.Should().NotBeNull();
        
        page1Result!.Page.Should().Be(1);
        page2Result!.Page.Should().Be(2);
        
        // Should have different stories (assuming there are enough stories)
        if (page1Result.TotalCount > 3)
        {
            var page1Ids = page1Result.Items.Select(s => s.Id).ToList();
            var page2Ids = page2Result.Items.Select(s => s.Id).ToList();
            page1Ids.Should().NotIntersectWith(page2Ids);
        }
    }

    [Fact]
    public async Task Get_StoriesSearch_WithValidQuery_ShouldReturnFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/stories/search?query=the&page=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        
        // All returned stories should contain the search term (case insensitive)
        foreach (var story in result.Items)
        {
            story.Title.Should().ContainEquivalentOf("the");
        }
    }

    [Fact]
    public async Task Get_StoriesSearch_WithEmptyQuery_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/stories/search?query=&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("The query field is required");
    }

    [Fact]
    public async Task Get_StoriesSearch_WithoutQuery_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/stories/search?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_StoriesSearch_WithCommonQuery_ShouldReturnResults()
    {
        // Act
        var response = await _client.GetAsync("/api/stories/search?query=a&page=1&pageSize=5");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_StoryById_WithValidId_ShouldReturnStory()
    {
        // Arrange - First get a list of stories to get a valid ID
        var storiesResponse = await _client.GetAsync("/api/stories?page=1&pageSize=1");
        storiesResponse.EnsureSuccessStatusCode();
        
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var storiesResult = await storiesResponse.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);
        
        storiesResult.Should().NotBeNull();
        storiesResult!.Items.Should().NotBeEmpty();
        var validStoryId = storiesResult.Items.First().Id;

        // Act
        var response = await _client.GetAsync($"/api/stories/{validStoryId}");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var story = await response.Content.ReadFromJsonAsync<Story>(jsonOptions);
        story.Should().NotBeNull();
        story!.Id.Should().Be(validStoryId);
        story.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_StoryById_WithInvalidId_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/stories/0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid story ID");
    }

    [Fact]
    public async Task Get_StoryById_WithNegativeId_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/stories/-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid story ID");
    }

    [Fact]
    public async Task Get_StoryById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Act - Use a very high ID that's unlikely to exist
        var response = await _client.GetAsync("/api/stories/999999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task Get_Stories_MultipleCalls_ShouldUseCaching()
    {
        // This test verifies that subsequent calls are faster due to caching
        // We can't directly test cache behavior in integration tests, but we can verify consistent results
        
        // Act
        var response1 = await _client.GetAsync("/api/stories?page=1&pageSize=3");
        var response2 = await _client.GetAsync("/api/stories?page=1&pageSize=3");

        // Assert
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result1 = await response1.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);
        var result2 = await response2.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        
        // Results should be identical (from cache)
        result1!.TotalCount.Should().Be(result2!.TotalCount);
        result1.Items.Should().HaveCount(result2.Items.Count());
        
        var ids1 = result1.Items.Select(s => s.Id).ToList();
        var ids2 = result2.Items.Select(s => s.Id).ToList();
        ids1.Should().Equal(ids2);
    }

    [Theory]
    [InlineData("react")]
    [InlineData("javascript")]
    [InlineData("python")]
    [InlineData("programming")]
    public async Task Get_StoriesSearch_WithDifferentQueries_ShouldReturnRelevantResults(string query)
    {
        // Act
        var response = await _client.GetAsync($"/api/stories/search?query={query}&page=1&pageSize=5");

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = await response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

            result.Should().NotBeNull();
            
            // If we have results, they should contain the search term
            foreach (var story in result!.Items)
            {
                story.Title.Should().ContainEquivalentOf(query);
            }
        }
        else
        {
            // If no results found, that's acceptable for some queries
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}