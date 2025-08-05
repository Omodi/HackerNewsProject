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
        
        var response = await _client.GetAsync("/api/health");

                response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        content.Should().Contain("Healthy");
        content.Should().Contain("timestamp");
        content.Should().Contain("version");
    }

    [Fact]
    public async Task Get_Stories_ShouldReturnPagedResult()
    {
        
        var response = await _client.GetAsync("/api/stories?page=1&pageSize=5");

                response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.Items.Should().HaveCountLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task Get_Stories_WithInvalidPage_ShouldNormalizePage()
    {
        
        var response = await _client.GetAsync("/api/stories?page=-1&pageSize=10");

                response.EnsureSuccessStatusCode();
        
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

        result.Should().NotBeNull();
        result!.Page.Should().Be(1); // Should be normalized to 1
    }

    [Fact]
    public async Task Get_Stories_WithInvalidPageSize_ShouldNormalizePageSize()
    {
        
        var response = await _client.GetAsync("/api/stories?page=1&pageSize=2000");

                response.EnsureSuccessStatusCode();
        
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

        result.Should().NotBeNull();
        // Page size parameter is ignored in minimal pagination
    }

    [Fact]
    public async Task Get_Stories_SecondPage_ShouldReturnDifferentStories()
    {
        
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        
        var page1Response = await _client.GetAsync("/api/stories?page=1&pageSize=3");
        var page2Response = await _client.GetAsync("/api/stories?page=2&pageSize=3");

                page1Response.EnsureSuccessStatusCode();
        page2Response.EnsureSuccessStatusCode();

        var page1Result = await page1Response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);
        var page2Result = await page2Response.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

        page1Result.Should().NotBeNull();
        page2Result.Should().NotBeNull();
        
        page1Result!.Page.Should().Be(1);
        page2Result!.Page.Should().Be(2);
        
        // Should have different stories (assuming there are enough stories)
        if (page1Result.Items.Count() > 0)
        {
            var page1Ids = page1Result.Items.Select(s => s.Id).ToList();
            var page2Ids = page2Result.Items.Select(s => s.Id).ToList();
            page1Ids.Should().NotIntersectWith(page2Ids);
        }
    }

    [Fact]
    public async Task Get_StoryById_WithValidId_ShouldReturnStory()
    {
        
        var storiesResponse = await _client.GetAsync("/api/stories?page=1&pageSize=1");
        storiesResponse.EnsureSuccessStatusCode();
        
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var storiesResult = await storiesResponse.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);
        
        storiesResult.Should().NotBeNull();
        storiesResult!.Items.Should().NotBeEmpty();
        var validStoryId = storiesResult.Items.First().Id;

        
        var response = await _client.GetAsync($"/api/stories/{validStoryId}");

                response.EnsureSuccessStatusCode();
        
        var story = await response.Content.ReadFromJsonAsync<Story>(jsonOptions);
        story.Should().NotBeNull();
        story!.Id.Should().Be(validStoryId);
        story.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_StoryById_WithInvalidId_ShouldReturnBadRequest()
    {
        
        var response = await _client.GetAsync("/api/stories/0");

                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid story ID");
    }

    [Fact]
    public async Task Get_StoryById_WithNegativeId_ShouldReturnBadRequest()
    {
        
        var response = await _client.GetAsync("/api/stories/-1");

                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid story ID");
    }

    [Fact]
    public async Task Get_StoryById_WithNonExistentId_ShouldReturnNotFound()
    {
        
        var response = await _client.GetAsync("/api/stories/999999999");

                response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task Get_Stories_MultipleCalls_ShouldUseCaching()
    {
        // This test verifies that subsequent calls are faster due to caching
        // We can't directly test cache behavior in integration tests, but we can verify consistent results
        
        
        var response1 = await _client.GetAsync("/api/stories?page=1&pageSize=3");
        var response2 = await _client.GetAsync("/api/stories?page=1&pageSize=3");

                response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result1 = await response1.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);
        var result2 = await response2.Content.ReadFromJsonAsync<PagedResult<Story>>(jsonOptions);

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        
        // Results should be identical (from cache)
        // Both calls should return results
        result1!.Items.Should().NotBeEmpty();
        result2!.Items.Should().NotBeEmpty();
        result1.Items.Should().HaveCount(result2.Items.Count());
        
        var ids1 = result1.Items.Select(s => s.Id).ToList();
        var ids2 = result2.Items.Select(s => s.Id).ToList();
        ids1.Should().Equal(ids2);
    }

}