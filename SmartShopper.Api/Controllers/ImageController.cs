using Microsoft.AspNetCore.Mvc;

namespace SmartShopper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly ILogger<ImageController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ImageController(ILogger<ImageController> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Ürün fotoğrafı yükle
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(5_000_000)] // 5MB limit
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Dosya seçilmedi" });
            }

            // Dosya tipi kontrolü
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(new { error = "Sadece JPG, PNG ve WebP formatları desteklenir" });
            }

            // Dosya boyutu kontrolü (5MB)
            if (file.Length > 5_000_000)
            {
                return BadRequest(new { error = "Dosya boyutu 5MB'dan küçük olmalı" });
            }

            // Uploads klasörünü oluştur
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "products");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Benzersiz dosya adı oluştur
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Dosyayı kaydet
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // URL oluştur - tam URL döndür
            var scheme = Request.Scheme; // http veya https
            var host = Request.Host.Value; // localhost:7013
            var imageUrl = $"{scheme}://{host}/uploads/products/{fileName}";

            _logger.LogInformation($"Ürün fotoğrafı yüklendi: {fileName}, URL: {imageUrl}");

            return Ok(new
            {
                success = true,
                imageUrl = imageUrl,
                fileName = fileName,
                size = file.Length,
                contentType = file.ContentType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Fotoğraf yükleme hatası: {ex.Message}");
            return StatusCode(500, new { error = "Fotoğraf yüklenirken hata oluştu" });
        }
    }

    /// <summary>
    /// Fotoğrafı sil
    /// </summary>
    [HttpDelete("{fileName}")]
    public IActionResult DeleteImage(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "products", fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "Dosya bulunamadı" });
            }

            System.IO.File.Delete(filePath);

            _logger.LogInformation($"Ürün fotoğrafı silindi: {fileName}");

            return Ok(new { success = true, message = "Fotoğraf silindi" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Fotoğraf silme hatası: {ex.Message}");
            return StatusCode(500, new { error = "Fotoğraf silinirken hata oluştu" });
        }
    }
}
