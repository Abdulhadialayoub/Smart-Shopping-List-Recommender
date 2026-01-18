using Microsoft.AspNetCore.Mvc;

namespace SmartShopper.Api.Controllers;

/// <summary>
/// n8n entegrasyonu için özel konfigürasyon endpoint'leri
/// </summary>
[ApiController]
[Route("api/n8n")]
[Produces("application/json")]
public class N8nController : ControllerBase
{
    /// <summary>
    /// n8n için API endpoint'lerinin listesini döndürür
    /// </summary>
    /// <returns>Kullanılabilir endpoint'ler</returns>
    [HttpGet("endpoints")]
    [ProducesResponseType(typeof(N8nEndpointsResponse), 200)]
    public IActionResult GetEndpoints()
    {
        var endpoints = new N8nEndpointsResponse
        {
            BaseUrl = $"{Request.Scheme}://{Request.Host}",
            Endpoints = new List<N8nEndpoint>
            {
                new N8nEndpoint
                {
                    Name = "Get Fridge Items",
                    Method = "GET",
                    Path = "/api/fridge/{userId}",
                    Description = "Kullanıcının buzdolabındaki tüm öğeleri getirir",
                    Parameters = new List<string> { "userId" }
                },
                new N8nEndpoint
                {
                    Name = "Add Fridge Item",
                    Method = "POST",
                    Path = "/api/fridge",
                    Description = "Buzdolabına yeni öğe ekler",
                    RequiredBody = true
                },
                new N8nEndpoint
                {
                    Name = "Get Recipe Suggestions",
                    Method = "GET",
                    Path = "/api/recipe/suggestions/{userId}",
                    Description = "Mevcut malzemelere göre tarif önerileri",
                    Parameters = new List<string> { "userId" }
                },
                new N8nEndpoint
                {
                    Name = "Compare Prices",
                    Method = "GET",
                    Path = "/api/shopping/compare-prices",
                    Description = "Ürün fiyatlarını karşılaştırır",
                    Parameters = new List<string> { "productName" }
                },
                new N8nEndpoint
                {
                    Name = "Fridge Summary",
                    Method = "GET",
                    Path = "/api/webhook/fridge-summary/{userId}",
                    Description = "Buzdolabı özet bilgileri (n8n için optimize edilmiş)",
                    Parameters = new List<string> { "userId" }
                },
                new N8nEndpoint
                {
                    Name = "Smart Shopping List",
                    Method = "POST",
                    Path = "/api/webhook/smart-shopping-list",
                    Description = "AI destekli akıllı alışveriş listesi oluşturur",
                    RequiredBody = true
                },
                new N8nEndpoint
                {
                    Name = "Batch Operations",
                    Method = "POST",
                    Path = "/api/webhook/batch-operation",
                    Description = "Toplu işlemler için endpoint",
                    RequiredBody = true
                }
            }
        };

        return Ok(endpoints);
    }

    /// <summary>
    /// n8n için örnek workflow şablonları
    /// </summary>
    /// <returns>Hazır workflow şablonları</returns>
    [HttpGet("workflow-templates")]
    [ProducesResponseType(typeof(List<N8nWorkflowTemplate>), 200)]
    public IActionResult GetWorkflowTemplates()
    {
        var templates = new List<N8nWorkflowTemplate>
        {
            new N8nWorkflowTemplate
            {
                Name = "Daily Expiry Check",
                Description = "Günlük son kullanma tarihi kontrolü ve bildirim",
                Category = "Automation",
                Nodes = new List<object>
                {
                    new
                    {
                        name = "Schedule Trigger",
                        type = "n8n-nodes-base.scheduleTrigger",
                        parameters = new { rule = new { interval = new[] { new { field = "hours", hoursInterval = 24 } } } }
                    },
                    new
                    {
                        name = "Get Expiring Items",
                        type = "n8n-nodes-base.httpRequest",
                        parameters = new
                        {
                            method = "GET",
                            url = $"{Request.Scheme}://{Request.Host}/api/fridge/demo-user-123/expiring?days=3"
                        }
                    }
                }
            },
            new N8nWorkflowTemplate
            {
                Name = "Smart Shopping List Creator",
                Description = "Buzdolabı durumuna göre akıllı alışveriş listesi oluşturur",
                Category = "Smart Automation",
                Nodes = new List<object>
                {
                    new
                    {
                        name = "Webhook Trigger",
                        type = "n8n-nodes-base.webhook",
                        parameters = new { path = "create-shopping-list" }
                    },
                    new
                    {
                        name = "Create Smart List",
                        type = "n8n-nodes-base.httpRequest",
                        parameters = new
                        {
                            method = "POST",
                            url = $"{Request.Scheme}://{Request.Host}/api/webhook/smart-shopping-list",
                            body = new { userId = "{{ $json.userId }}" }
                        }
                    }
                }
            }
        };

        return Ok(templates);
    }

    /// <summary>
    /// n8n için test endpoint'i
    /// </summary>
    /// <returns>Test sonucu</returns>
    [HttpGet("test")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult TestConnection()
    {
        return Ok(new
        {
            Status = "Connected",
            Message = "Smart Shopper API n8n entegrasyonu çalışıyor",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            AvailableEndpoints = 7
        });
    }
}

// n8n için model sınıfları
public class N8nEndpointsResponse
{
    public string BaseUrl { get; set; } = string.Empty;
    public List<N8nEndpoint> Endpoints { get; set; } = new();
}

public class N8nEndpoint
{
    public string Name { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Parameters { get; set; } = new();
    public bool RequiredBody { get; set; } = false;
}

public class N8nWorkflowTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<object> Nodes { get; set; } = new();
}