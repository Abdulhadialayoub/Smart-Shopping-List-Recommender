using Microsoft.AspNetCore.Mvc;
using SmartShopper.Api.Models;
using SmartShopper.Api.Services;
using SmartShopper.Api.Services.Data;

namespace SmartShopper.Api.Controllers;

/// <summary>
/// Kullanıcı yönetimi için API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly IDataService _dataService;

    public UserController(IDataService firebaseService)
    {
        _dataService = firebaseService;
    }

    /// <summary>
    /// Telegram kullanıcısını Chat ID ile getir
    /// </summary>
    [HttpGet("telegram/{chatId}")]
    public async Task<ActionResult> GetUserByTelegramChatId(string chatId)
    {
        try
        {
            var user = await _dataService.GetUserByTelegramIdAsync(chatId);
            if (user != null)
            {
                return Ok(new { 
                    id = user.Id, 
                    telegramChatId = user.TelegramChatId,
                    name = user.Name,
                    email = user.Email
                });
            }
            return NotFound(new { exists = false, message = "Kullanıcı bulunamadı" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Kullanıcı kontrolü başarısız: " + ex.Message });
        }
    }

    /// <summary>
    /// Yeni Telegram kullanıcısı oluştur
    /// </summary>
    [HttpPost("telegram")]
    public async Task<ActionResult> CreateTelegramUser([FromBody] TelegramUserRequest request)
    {
        try
        {
            // Önce mevcut kullanıcı var mı kontrol et
            var existingUser = await _dataService.GetUserByTelegramIdAsync(request.TelegramChatId);
            if (existingUser != null)
            {
                return Ok(new { 
                    success = true, 
                    message = "Kullanıcı zaten mevcut",
                    user = new {
                        id = existingUser.Id,
                        telegramChatId = existingUser.TelegramChatId,
                        name = existingUser.Name
                    }
                });
            }

            // Yeni kullanıcı oluştur
            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                TelegramChatId = request.TelegramChatId,
                TelegramUsername = request.Username,
                Name = request.FirstName ?? "Telegram User",
                Email = $"telegram_{request.TelegramChatId}@smartshopper.local",
                CreatedAt = DateTime.UtcNow
            };

            await _dataService.CreateUserAsync(newUser);

            return Ok(new 
            { 
                success = true, 
                message = "Kullanıcı başarıyla oluşturuldu",
                user = new 
                {
                    id = newUser.Id,
                    telegramChatId = newUser.TelegramChatId,
                    username = request.Username,
                    firstName = request.FirstName
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Kullanıcı oluşturma başarısız: " + ex.Message });
        }
    }

    /// <summary>
    /// n8n için aktif kullanıcı listesi (Telegram Chat ID dahil)
    /// </summary>
    [HttpGet("n8n/active-users")]
    public async Task<ActionResult> GetActiveUsers()
    {
        try
        {
            // Gerçek uygulamada Firebase'den aktif kullanıcıları çekeceğiz
            // Şimdilik demo kullanıcıları döndürüyoruz
            var activeUsers = new[]
            {
                new { 
                    id = "demo-user-123", 
                    name = "Demo Kullanıcı", 
                    email = "demo@smartshopper.com",
                    telegramChatId = "1001914546", // Senin chat ID'n
                    telegramUsername = "Axbod01",
                    lastActive = DateTime.UtcNow.AddHours(-2),
                    isActive = true
                },
                new { 
                    id = "test-user-456", 
                    name = "Test Kullanıcı", 
                    email = "test@smartshopper.com",
                    telegramChatId = (string?)null, // Telegram bağlamadı
                    telegramUsername = (string?)null,
                    lastActive = DateTime.UtcNow.AddHours(-1),
                    isActive = true
                }
            };

            return Ok(new
            {
                users = activeUsers,
                totalUsers = activeUsers.Length,
                activeUsers = activeUsers.Count(u => u.isActive),
                usersWithTelegram = activeUsers.Count(u => !string.IsNullOrEmpty(u.telegramChatId)),
                retrievedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Kullanıcı listesi alınamadı");
        }
    }

    /// <summary>
    /// Kullanıcının buzdolabı özeti (n8n için)
    /// </summary>
    [HttpGet("{userId}/fridge-summary")]
    public async Task<ActionResult> GetFridgeSummary(string userId)
    {
        try
        {
            var fridgeItems = await _dataService.GetFridgeItemsAsync(userId);
            var today = DateTime.UtcNow;

            // Son kullanma tarihi analizleri
            var expiringItems = fridgeItems.Where(item => 
                (item.ExpiryDate - today).TotalDays <= 3 && 
                (item.ExpiryDate - today).TotalDays >= 0).ToList();

            var expiredItems = fridgeItems.Where(item => 
                item.ExpiryDate < today).ToList();

            // Kategori analizi
            var categoryCount = fridgeItems
                .GroupBy(item => item.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            // Eksik temel ürünler
            var basicItems = new[] { "Ekmek", "Süt", "Yumurta", "Domates", "Soğan", "Patates" };
            var availableItems = fridgeItems.Select(item => item.Name.ToLower()).ToList();
            var missingBasicItems = basicItems.Where(basic => 
                !availableItems.Any(available => 
                    available.Contains(basic.ToLower()) || 
                    basic.ToLower().Contains(available))).ToList();

            return Ok(new
            {
                userId,
                summary = new
                {
                    totalItems = fridgeItems.Count,
                    expiringItems = expiringItems.Count,
                    expiredItems = expiredItems.Count,
                    missingBasicItems = missingBasicItems.Count,
                    categories = categoryCount.Keys.ToList(),
                    categoryCount
                },
                details = new
                {
                    expiringItems = expiringItems.Select(item => new
                    {
                        item.Name,
                        item.Category,
                        item.ExpiryDate,
                        daysUntilExpiry = Math.Ceiling((item.ExpiryDate - today).TotalDays)
                    }),
                    expiredItems = expiredItems.Select(item => new
                    {
                        item.Name,
                        item.Category,
                        item.ExpiryDate,
                        daysSinceExpiry = Math.Ceiling((today - item.ExpiryDate).TotalDays)
                    }),
                    missingBasicItems
                },
                needsAttention = expiringItems.Count > 0 || expiredItems.Count > 0 || missingBasicItems.Count > 3,
                healthScore = CalculateFridgeHealthScore(fridgeItems.Count, expiringItems.Count, expiredItems.Count, missingBasicItems.Count),
                lastUpdated = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Buzdolabı özeti alınamadı");
        }
    }

    /// <summary>
    /// Kullanıcının alışveriş geçmişi analizi (n8n için)
    /// </summary>
    [HttpGet("{userId}/shopping-analytics")]
    public async Task<ActionResult> GetShoppingAnalytics(string userId)
    {
        try
        {
            var shoppingLists = await _dataService.GetShoppingListsAsync(userId);
            var completedLists = shoppingLists.Where(list => list.IsCompleted).ToList();
            
            // Son 30 günlük analiz
            var last30Days = DateTime.UtcNow.AddDays(-30);
            var recentLists = completedLists.Where(list => list.CreatedAt >= last30Days).ToList();

            // En çok satın alınan ürünler
            var allItems = recentLists.SelectMany(list => list.Items).ToList();
            var topProducts = allItems
                .GroupBy(item => item.Name.ToLower())
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new { product = g.Key, count = g.Count() })
                .ToList();

            // Ortalama alışveriş tutarı
            var averageTotal = recentLists.Any() ? recentLists.Average(list => list.EstimatedTotal) : 0;

            return Ok(new
            {
                userId,
                analytics = new
                {
                    totalShoppingLists = shoppingLists.Count,
                    completedLists = completedLists.Count,
                    recentListsCount = recentLists.Count,
                    averageTotal,
                    topProducts,
                    shoppingFrequency = recentLists.Count / 4.0, // Haftalık ortalama
                    completionRate = shoppingLists.Count > 0 ? (double)completedLists.Count / shoppingLists.Count * 100 : 0
                },
                recommendations = new
                {
                    suggestedFrequency = "Haftada 2 kez",
                    budgetRecommendation = averageTotal > 0 ? $"Ortalama {averageTotal:F2} TL bütçe ayırın" : "Bütçe verisi yetersiz",
                    topCategories = topProducts.Take(3).Select(p => p.product).ToList()
                },
                lastUpdated = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Alışveriş analizi alınamadı");
        }
    }

    private int CalculateFridgeHealthScore(int totalItems, int expiringItems, int expiredItems, int missingBasicItems)
    {
        int score = 100;
        
        // Süresi geçmiş ürünler için puan düşür
        score -= expiredItems * 10;
        
        // Yakında bitecek ürünler için puan düşür
        score -= expiringItems * 5;
        
        // Eksik temel ürünler için puan düşür
        score -= missingBasicItems * 3;
        
        // Çok az ürün varsa puan düşür
        if (totalItems < 5) score -= 20;
        
        // Çok fazla ürün varsa (israf riski) puan düşür
        if (totalItems > 50) score -= 10;
        
        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Kullanıcı profilini güncelle
    /// </summary>
    [HttpPut("{userId}/profile")]
    public async Task<ActionResult> UpdateProfile(string userId, [FromBody] UpdateProfileRequest request)
    {
        try
        {
            var user = await _dataService.GetUserAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı" });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                user.Name = request.Name;
            }

            if (!string.IsNullOrWhiteSpace(request.TelegramChatId))
            {
                user.TelegramChatId = request.TelegramChatId;
            }

            if (!string.IsNullOrWhiteSpace(request.TelegramUsername))
            {
                user.TelegramUsername = request.TelegramUsername;
            }

            await _dataService.UpdateUserAsync(user);

            return Ok(new
            {
                success = true,
                message = "Profil başarıyla güncellendi",
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    name = user.Name,
                    telegramChatId = user.TelegramChatId,
                    telegramUsername = user.TelegramUsername
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Profil güncellenirken bir hata oluştu", error = ex.Message });
        }
    }

    /// <summary>
    /// Kullanıcı profilini getir
    /// </summary>
    [HttpGet("{userId}/profile")]
    public async Task<ActionResult> GetProfile(string userId)
    {
        try
        {
            var user = await _dataService.GetUserAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı" });
            }

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                name = user.Name,
                telegramChatId = user.TelegramChatId,
                telegramUsername = user.TelegramUsername,
                createdAt = user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Profil getirilirken bir hata oluştu", error = ex.Message });
        }
    }
}

public class UpdateProfileRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? TelegramChatId { get; set; }
    public string? TelegramUsername { get; set; }
}

public class TelegramUserRequest
{
    public string TelegramChatId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
