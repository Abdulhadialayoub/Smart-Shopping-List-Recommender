using Microsoft.AspNetCore.Mvc;
using SmartShopper.Api.Models;
using SmartShopper.Api.Services.Data;
using BCrypt.Net;

namespace SmartShopper.Api.Controllers;

/// <summary>
/// Kullanıcı kimlik doğrulama ve yetkilendirme için API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IDataService _dataService;

    public AuthController(IDataService dataService)
    {
        _dataService = dataService;
    }

    /// <summary>
    /// Kullanıcı girişi
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Email ile kullanıcıyı bul
            var user = await _dataService.GetUserByEmailAsync(request.Email);
            
            if (user == null)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Bu email adresi ile kayıtlı kullanıcı bulunamadı. Lütfen önce kayıt olun."
                });
            }

            // Şifre kontrolü
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Email veya şifre hatalı."
                });
            }
            
            return Ok(new AuthResponse
            {
                User = user,
                Token = GenerateToken(user),
                Success = true,
                Message = "Giriş başarılı"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Giriş işlemi sırasında bir hata oluştu: " + ex.Message
            });
        }
    }

    /// <summary>
    /// Kullanıcı kaydı
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Email kontrolü
            var existingUser = await _dataService.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Bu email adresi zaten kullanımda"
                });
            }

            // Şifre hash'le
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Yeni kullanıcı oluştur
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = request.Email,
                Name = request.Name,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            await _dataService.CreateUserAsync(user);

            return CreatedAtAction(nameof(GetProfile), new { userId = user.Id }, new AuthResponse
            {
                User = user,
                Token = GenerateToken(user),
                Success = true,
                Message = "Hesap başarıyla oluşturuldu"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Hesap oluşturma işlemi sırasında bir hata oluştu: " + ex.Message
            });
        }
    }

    /// <summary>
    /// Şifre sıfırlama
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    public ActionResult<AuthResponse> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // Demo için sadece başarılı mesaj döndür
        // Gerçek uygulamada email gönderme işlemi yapılacak
        
        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Şifre sıfırlama bağlantısı email adresinize gönderildi"
        });
    }

    /// <summary>
    /// Test endpoint
    /// </summary>
    [HttpGet("test")]
    public ActionResult<string> Test()
    {
        return Ok("API is working!");
    }

    /// <summary>
    /// Kullanıcı profili
    /// </summary>
    [HttpGet("profile/{userId}")]
    [ProducesResponseType(typeof(User), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<User>> GetProfile(string userId)
    {
        var user = await _dataService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    private string GenerateToken(User user)
    {
        // Basit token oluşturma (gerçek uygulamada JWT kullanılacak)
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user.Id}:{user.Email}:{DateTime.UtcNow.Ticks}"));
    }
}

// Request/Response modelleri
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class AuthResponse
{
    public User? User { get; set; }
    public string? Token { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
