using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoriesController : ControllerBase
{
    private readonly IHackerNewsService _hackerNewsService;
    private readonly ILogger<StoriesController> _logger;

    public StoriesController(IHackerNewsService hackerNewsService, ILogger<StoriesController> logger)
    {
        _hackerNewsService = hackerNewsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<Story>>> GetStories(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var (validPage, validPageSize) = ValidatePagination(page, pageSize);

            _logger.LogInformation("Getting stories - Page: {Page}, PageSize: {PageSize}", validPage, validPageSize);
            
            var result = await _hackerNewsService.GetStoriesAsync(validPage, validPageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stories - Page: {Page}, PageSize: {PageSize}", page, pageSize);
            return StatusCode(500, "An error occurred while fetching stories");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Story>> GetStory(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest("Invalid story ID");
            }

            _logger.LogInformation("Getting story with ID: {StoryId}", id);
            
            var story = await _hackerNewsService.GetStoryAsync(id);
            
            if (story == null)
            {
                return NotFound($"Story with ID {id} not found");
            }

            return Ok(story);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting story with ID: {StoryId}", id);
            return StatusCode(500, "An error occurred while fetching the story");
        }
    }

    private static (int page, int pageSize) ValidatePagination(int page, int pageSize)
    {
        var validPage = page < 1 ? 1 : page;
        var validPageSize = pageSize < 1 || pageSize > 1000 ? 20 : pageSize;
        return (validPage, validPageSize);
    }
}