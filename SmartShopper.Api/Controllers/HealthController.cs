using Microsoft.AspNetCore.Mvc;

namespace SmartShopper.Api.Controllers;

/// <summary>
/// API sağlık durumu kontrolü için endpoint'ler
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// API'nin çalışır durumda olup olmadığını kontrol eder
    /// </summary>
    /// <returns>API durumu</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        });
    }

    /// <summary>
    /// API versiyonu bilgisini döndürür
    /// </summary>
    /// <returns>Versiyon bilgisi</returns>
    [HttpGet("version")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetVersion()
    {
        return Ok(new
        {
            Version = "1.0.0",
            ApiName = "Smart Shopper API",
            BuildDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Features = new[]
            {
                "Buzdolabı Yönetimi",
                "Tarif Önerileri",
                "Alışveriş Listesi",
                "Fiyat Karşılaştırması"
            }
        });
    }
}