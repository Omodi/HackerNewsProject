using HackerNewsApi.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly DatabaseMaintenanceService _dbMaintenanceService;

    public HealthController(DatabaseMaintenanceService dbMaintenanceService)
    {
        _dbMaintenanceService = dbMaintenanceService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }

    [HttpGet("database")]
    public async Task<IActionResult> GetDatabaseHealth()
    {
        try
        {
            var stats = await _dbMaintenanceService.GetCurrentStatsAsync();
            
            var status = stats.IsOverLimit ? "critical" :
                        stats.IsNearLimit ? "warning" : "healthy";

            return Ok(new
            {
                Status = status,
                Database = new
                {
                    SizeMB = Math.Round(stats.SizeInMB, 2),
                    UsagePercentage = Math.Round(stats.UsagePercentage, 1),
                    MaxSizeMB = 800,
                    StoryCount = stats.StoryCount,
                    OldestStory = stats.OldestStoryDate?.ToString("yyyy-MM-dd HH:mm"),
                    NewestStory = stats.NewestStoryDate?.ToString("yyyy-MM-dd HH:mm"),
                    NeedsCleanup = stats.ShouldCleanupOldData
                },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Status = "error",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("database/cleanup")]
    public async Task<IActionResult> TriggerDatabaseCleanup()
    {
        try
        {
            var statsBefore = await _dbMaintenanceService.GetCurrentStatsAsync();
            
            await _dbMaintenanceService.ForceCleanupAsync();
            
            var statsAfter = await _dbMaintenanceService.GetCurrentStatsAsync();
            
            return Ok(new
            {
                Status = "completed",
                Before = new
                {
                    SizeMB = Math.Round(statsBefore.SizeInMB, 2),
                    StoryCount = statsBefore.StoryCount
                },
                After = new
                {
                    SizeMB = Math.Round(statsAfter.SizeInMB, 2),
                    StoryCount = statsAfter.StoryCount
                },
                Saved = new
                {
                    SizeMB = Math.Round(statsBefore.SizeInMB - statsAfter.SizeInMB, 2),
                    Stories = statsBefore.StoryCount - statsAfter.StoryCount
                },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Status = "error",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}