using Microsoft.AspNetCore.Mvc;
using SmartShopper.Api.Services;

namespace SmartShopper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly ScraperService _scraper;

    public ScraperController(ScraperService scraper)
    {
        _scraper = scraper;
    }

    [HttpGet("fetch")]
    public async Task<IActionResult> Fetch([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("url param required");

        try
        {
            var html = await _scraper.GetHtmlAsync(url);
            return Ok(new { html, length = html.Length, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}
