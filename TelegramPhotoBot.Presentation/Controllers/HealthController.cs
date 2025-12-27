using Microsoft.AspNetCore.Mvc;

namespace TelegramPhotoBot.Presentation.Controllers;

/// <summary>
/// Health check endpoint to keep the application alive
/// </summary>
[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Simple health check endpoint that returns OK
    /// Used by scheduled jobs to keep the application alive
    /// </summary>
    [HttpGet("")]
    [HttpGet("health")]
    [HttpGet("api/health")]
    public IActionResult Get()
    {
        return Ok(new { status = "ok", timestamp = DateTime.UtcNow });
    }
}

