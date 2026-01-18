using SmartShopper.Api.Services;
using SmartShopper.Api.Data;
using SmartShopper.Api.Services.Data;
using SmartShopper.Api.Services.DualModel;
using SmartShopper.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddMemoryCache(); // For image caching
builder.Services.AddSingleton<ScraperService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Smart Shopper API",
        Version = "v1",
        Description = "Akıllı Alışveriş Listesi ve Tarif Önerici API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Smart Shopper Team",
            Email = "info@smartshopper.com"
        }
    });
    
    // XML yorumlarını etkinleştir
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://localhost:5173", "http://localhost:5174", "https://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// SQL Server configuration - use environment variable if available
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Data service
builder.Services.AddScoped<IDataService, SqlDataService>();


builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddHttpClient<RecipeService>();

builder.Services.AddScoped<IPriceComparisonService, PriceComparisonService>();
builder.Services.AddHttpClient<PriceComparisonService>();

// GetirApiService ve TrendyolApiService kaldırıldı - sadece Cimri kullanılıyor

builder.Services.AddSingleton<PriceCacheService>();

builder.Services.AddScoped<GeminiApiService>();
builder.Services.AddHttpClient<GeminiApiService>();

builder.Services.AddScoped<GroqApiService>();
builder.Services.AddHttpClient<GroqApiService>();

builder.Services.AddScoped<INutritionApiService, NutritionApiService>();
builder.Services.AddHttpClient<NutritionApiService>();

// Cimri Scraper configuration
builder.Services.Configure<CimriScraperOptions>(
    builder.Configuration.GetSection("CimriScraper"));

// Dual-Model Verification configuration
builder.Services.Configure<DualModelVerificationOptions>(
    builder.Configuration.GetSection("DualModelVerification"));
builder.Services.Configure<GroqOptions>(
    builder.Configuration.GetSection("Groq"));
builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<OpenAIOptions>(
    builder.Configuration.GetSection("OpenAI"));

// Validate required API keys for Dual-Model Verification
// Priority: Environment variables > appsettings.json
var groqApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") 
    ?? builder.Configuration["Groq:ApiKey"];
var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
    ?? builder.Configuration["Gemini:ApiKey"];
var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
    ?? builder.Configuration["OpenAI:ApiKey"];

// Override configuration with environment variables if present
if (!string.IsNullOrWhiteSpace(groqApiKey))
{
    builder.Configuration["Groq:ApiKey"] = groqApiKey;
}
if (!string.IsNullOrWhiteSpace(geminiApiKey))
{
    builder.Configuration["Gemini:ApiKey"] = geminiApiKey;
}
if (!string.IsNullOrWhiteSpace(openAIApiKey))
{
    builder.Configuration["OpenAI:ApiKey"] = openAIApiKey;
}

// Override other sensitive configs from environment variables
var firebaseProjectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
var firebaseKeyPath = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_KEY_PATH");
var nutritionApiKey = Environment.GetEnvironmentVariable("NUTRITION_API_KEY");
var telegramBotToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");

if (!string.IsNullOrWhiteSpace(firebaseProjectId))
{
    builder.Configuration["Firebase:ProjectId"] = firebaseProjectId;
}
if (!string.IsNullOrWhiteSpace(firebaseKeyPath))
{
    builder.Configuration["Firebase:ServiceAccountKeyPath"] = firebaseKeyPath;
}
if (!string.IsNullOrWhiteSpace(nutritionApiKey))
{
    builder.Configuration["NutritionAPI:ApiKey"] = nutritionApiKey;
}
if (!string.IsNullOrWhiteSpace(telegramBotToken))
{
    builder.Configuration["Telegram:BotToken"] = telegramBotToken;
}

if (string.IsNullOrWhiteSpace(groqApiKey))
{
    throw new InvalidOperationException(
        "GROQ_API_KEY is not configured. Please set the 'Groq:ApiKey' configuration value or the GROQ_API_KEY environment variable.");
}

// At least one validator (Gemini or OpenAI) must be configured
if (string.IsNullOrWhiteSpace(geminiApiKey) && string.IsNullOrWhiteSpace(openAIApiKey))
{
    throw new InvalidOperationException(
        "At least one validator API key must be configured. Please set either 'Gemini:ApiKey' (GEMINI_API_KEY) or 'OpenAI:ApiKey' (OPENAI_API_KEY).");
}

// Log which validator is being used
if (!string.IsNullOrWhiteSpace(openAIApiKey))
{
    Console.WriteLine("✅ OpenAI API key found - OpenAI will be used as validator");
}
if (!string.IsNullOrWhiteSpace(geminiApiKey))
{
    Console.WriteLine("✅ Gemini API key found - Gemini will be used as validator");
}
if (!string.IsNullOrWhiteSpace(openAIApiKey) && !string.IsNullOrWhiteSpace(geminiApiKey))
{
    Console.WriteLine("⚠️  Both validators configured - OpenAI will be preferred");
}

// Register singleton HttpClient for Cimri scraper
builder.Services.AddHttpClient<CimriHttpClientService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    });

// Register services
builder.Services.AddSingleton<IUserAgentProvider, UserAgentProvider>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<ICimriHtmlParserService, CimriHtmlParserService>();
builder.Services.AddScoped<ICimriScraperService, CimriScraperService>();

// Smart Product Selector Service
builder.Services.AddScoped<ISmartProductSelectorService, SmartProductSelectorService>();

// AI Product Selector Service
builder.Services.AddScoped<IAIProductSelectorService, AIProductSelectorService>();

// Smart Product Matching Service (3-Stage: Query Expansion + Multi-Result + AI Re-ranking)
builder.Services.AddScoped<ISmartProductMatchingService, SmartProductMatchingService>();

// Dual-Model Verification Services
builder.Services.AddScoped<IGroqService, GroqService>();
builder.Services.AddHttpClient<GroqService>();

// Register validators based on configuration
if (!string.IsNullOrWhiteSpace(openAIApiKey))
{
    builder.Services.AddScoped<IOpenAIService, OpenAIService>();
    Console.WriteLine("✅ OpenAI service registered");
}

if (!string.IsNullOrWhiteSpace(geminiApiKey))
{
    builder.Services.AddScoped<IGeminiService, GeminiService>();
    builder.Services.AddHttpClient<GeminiService>();
    Console.WriteLine("✅ Gemini service registered");
}

builder.Services.AddScoped<VerificationCacheService>();
builder.Services.AddScoped<IDualModelVerificationService, DualModelVerificationService>();

// Input Validation Service
builder.Services.AddScoped<IInputValidationService, InputValidationService>();

// Output Sanitization Service
builder.Services.AddScoped<IOutputSanitizationService, OutputSanitizationService>();

// Telegram Bot Service
// Polling mode: Sürekli mesaj kontrol eder (kaynak kullanır)
// Webhook mode: Sadece mesaj geldiğinde çalışır (verimli)
// n8n entegrasyonu için Polling kapalı, ancak Notification için Servis açık
builder.Services.AddSingleton<TelegramBotService>();
// builder.Services.AddHostedService(provider => provider.GetRequiredService<TelegramBotService>());

var app = builder.Build();

// Otomatik Migration uygula
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı migration hatası");
    }
}

// Configure the HTTP request pipeline
// Swagger'ı her ortamda etkinleştir
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Shopper API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Smart Shopper API Documentation";
    c.DefaultModelsExpandDepth(-1);
    c.DisplayRequestDuration();
});

// HTTPS redirect'i devre dışı bırak (development için)
// app.UseHttpsRedirection();

// Static files (wwwroot)
app.UseStaticFiles();

// Rate limiting middleware
app.UseRateLimiting();

app.UseCors("AllowReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
