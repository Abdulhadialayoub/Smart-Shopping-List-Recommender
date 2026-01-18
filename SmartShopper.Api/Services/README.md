# Services Klasör Yapısı

## 📁 Organize Edilmiş Servis Mimarisi

### 🔌 Interfaces/
Interface tanımlamaları - Dependency Injection için
- `IFirebaseService.cs` - Firebase database operations
- `IRecipeService.cs` - Recipe suggestions & generation
- `IPriceComparisonService.cs` - Price comparison logic
- `ICimriScraperService.cs` - Cimri scraper interface
- `ICimriHtmlParserService.cs` - HTML parsing interface
- `ICacheService.cs` - Caching interface
- `IUserAgentProvider.cs` - User agent provider interface

### 🔥 Firebase/
Firebase entegrasyonu - Database operations
- `FirebaseService.cs` - Firebase CRUD operations
- `FirebaseInitializer.cs` - Firebase configuration & initialization

### 🤖 AI/
Yapay zeka servisleri - Gemini AI entegrasyonu
- `GeminiApiService.cs` - Gemini AI API client
- `RecipeService.cs` - AI-powered recipe suggestions

### 🛒 Cimri/
Cimri.com scraper servisleri - Fiyat karşılaştırma
- `CimriScraperService.cs` - Main scraper service
- `CimriHtmlParserService.cs` - HTML parsing logic
- `CimriHttpClientService.cs` - HTTP client wrapper
- `CimriScraperOptions.cs` - Configuration options
- `UserAgentProvider.cs` - User agent rotation
- `TurkishCharacterHelper.cs` - Turkish character handling

### 🌐 External/
Dış API entegrasyonları
- `NutritionApiService.cs` - USDA FoodData Central API
- `TelegramBotService.cs` - Telegram bot integration

### ⚙️ Implementations/
Genel servis implementasyonları
- `PriceComparisonService.cs` - Price comparison orchestration
- `CacheService.cs` - General caching service
- `PriceCacheService.cs` - Price-specific caching
- `ScraperService.cs` - Playwright scraper (legacy)

## 🔄 Namespace Yapısı

```csharp
SmartShopper.Api.Services.Interfaces     // Interface'ler
SmartShopper.Api.Services.Firebase       // Firebase servisleri
SmartShopper.Api.Services.AI             // AI servisleri
SmartShopper.Api.Services.Cimri          // Cimri scraper
SmartShopper.Api.Services.External       // Dış API'ler
SmartShopper.Api.Services.Implementations // Genel implementasyonlar
```

## 📝 Kullanım

Program.cs'de DI registration:
```csharp
// Interfaces
builder.Services.AddScoped<IFirebaseService, FirebaseService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IPriceComparisonService, PriceComparisonService>();

// Cimri
builder.Services.AddScoped<ICimriScraperService, CimriScraperService>();

// External
builder.Services.AddScoped<INutritionApiService, NutritionApiService>();
```
