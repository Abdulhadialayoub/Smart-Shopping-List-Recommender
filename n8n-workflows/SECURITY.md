# Güvenlik Politikası

## Hassas Bilgilerin Yönetimi

Bu proje, API anahtarları ve diğer hassas bilgileri environment variables ve configuration dosyaları aracılığıyla yönetir.

### ✅ Yapılması Gerekenler

1. **Environment Variables Kullanın**
   - Tüm API anahtarlarını `.env` dosyalarında saklayın
   - `.env` dosyalarını asla Git'e commit etmeyin
   - `.env.example` dosyalarını referans olarak kullanın

2. **Configuration Dosyaları**
   - `appsettings.json` dosyalarında API anahtarlarını boş bırakın
   - Production değerlerini environment variables ile override edin
   - Firebase service account key dosyalarını `.gitignore`'a ekleyin

3. **n8n Workflows**
   - Telegram bot token'larını placeholder olarak bırakın: `<YOUR_BOT_TOKEN>`
   - Chat ID'leri placeholder olarak bırakın: `<YOUR_CHAT_ID>`
   - Import ettikten sonra n8n içinde credentials ekleyin

### ❌ Yapılmaması Gerekenler

- API anahtarlarını kaynak koduna hardcode etmeyin
- Token'ları commit mesajlarına yazmayın
- Hassas bilgileri log'lamayın
- Production credentials'ı development ortamında kullanmayın

## Gerekli Environment Variables

### Backend (SmartShopper.Api)

```bash
# Database
CONNECTION_STRING=your-connection-string

# Firebase
FIREBASE_PROJECT_ID=your-project-id
FIREBASE_SERVICE_ACCOUNT_KEY_PATH=path-to-key.json

# AI Services
OPENAI_API_KEY=your-openai-key
GEMINI_API_KEY=your-gemini-key
GROQ_API_KEY=your-groq-key

# Nutrition API
NUTRITION_API_KEY=your-nutrition-key

# Telegram
TELEGRAM_BOT_TOKEN=your-bot-token
```

### Frontend

```bash
VITE_API_URL=https://your-api-url
VITE_N8N_WEBHOOK_URL=your-webhook-url
```

## Güvenlik Kontrol Listesi

- [x] `.gitignore` dosyaları oluşturuldu
- [x] API anahtarları configuration dosyalarından temizlendi
- [x] n8n workflow'larındaki token'lar placeholder'a çevrildi
- [x] `.env.example` dosyaları oluşturuldu
- [x] README'de güvenlik uyarıları eklendi

## Güvenlik Açığı Bildirimi

Bir güvenlik açığı bulursanız, lütfen GitHub Issues üzerinden bildirin.

## Lisans

Bu proje MIT lisansı altında lisanslanmıştır.
