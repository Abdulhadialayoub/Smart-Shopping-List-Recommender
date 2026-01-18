# Smart Shopper - Akıllı Alışveriş Asistanı

Yapay zeka destekli akıllı alışveriş ve tarif önerisi platformu. Kullanıcıların buzdolabındaki malzemelere göre tarif önerir ve eksik malzemelerin en uygun fiyatlarını bulur.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![React](https://img.shields.io/badge/React-18-61DAFB?style=for-the-badge&logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-5.0-3178C6?style=for-the-badge&logo=typescript&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Firebase](https://img.shields.io/badge/Firebase-FFCA28?style=for-the-badge&logo=firebase&logoColor=black)
![OpenAI](https://img.shields.io/badge/OpenAI-412991?style=for-the-badge&logo=openai&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)

## 🚀 Özellikler

- **AI Destekli Tarif Önerileri**: Buzdolabınızdaki malzemelere göre özel tarifler
- **Dual-Model Doğrulama**: Groq (hızlı) + OpenAI/Gemini (güçlü) kombinasyonu
- **Akıllı Fiyat Karşılaştırma**: Cimri.com entegrasyonu ile en uygun fiyatları bulma
- **Telegram Bot**: Telegram üzerinden kolay erişim
- **Besin Değeri Analizi**: USDA API ile detaylı besin bilgileri
- **Firebase Entegrasyonu**: Güvenli kullanıcı yönetimi

## 🛠️ Teknolojiler

### Backend
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=c-sharp&logoColor=white)
![Entity Framework](https://img.shields.io/badge/Entity_Framework-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=flat-square&logo=microsoft-sql-server&logoColor=white)

### Frontend
![React](https://img.shields.io/badge/React-18-61DAFB?style=flat-square&logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-5.0-3178C6?style=flat-square&logo=typescript&logoColor=white)
![Vite](https://img.shields.io/badge/Vite-646CFF?style=flat-square&logo=vite&logoColor=white)
![TailwindCSS](https://img.shields.io/badge/Tailwind_CSS-38B2AC?style=flat-square&logo=tailwind-css&logoColor=white)

### AI/ML
![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4o--mini-412991?style=flat-square&logo=openai&logoColor=white)
![Google](https://img.shields.io/badge/Google-Gemini_2.0-4285F4?style=flat-square&logo=google&logoColor=white)
![Groq](https://img.shields.io/badge/Groq-Llama_3.1-FF6B6B?style=flat-square&logo=meta&logoColor=white)

### DevOps & Tools
![Docker](https://img.shields.io/badge/Docker-2496ED?style=flat-square&logo=docker&logoColor=white)
![Firebase](https://img.shields.io/badge/Firebase-FFCA28?style=flat-square&logo=firebase&logoColor=black)
![n8n](https://img.shields.io/badge/n8n-EA4B71?style=flat-square&logo=n8n&logoColor=white)
![Telegram](https://img.shields.io/badge/Telegram-26A5E4?style=flat-square&logo=telegram&logoColor=white)
![Git](https://img.shields.io/badge/Git-F05032?style=flat-square&logo=git&logoColor=white)

## 📋 Gereksinimler

- .NET 8.0 SDK
- Node.js 18+
- SQL Server
- Firebase Account
- API Keys:
  - OpenAI API Key
  - Google Gemini API Key
  - Groq API Key
  - USDA Nutrition API Key
  - Telegram Bot Token

## 🔧 Kurulum

### 1. Repository'yi klonlayın

```bash
git clone https://github.com/Abdulhadialayoub/Smart-Shopping-List-Recommender.git
cd Smart-Shopping-List-Recommender
```

### 2. Environment Variables Ayarlayın

Her component için ayrı `.env` dosyaları oluşturun:

#### Backend (SmartShopper.Api)

```bash
cd SmartShopper.Api
cp .env.example .env
# .env dosyasını düzenleyin ve API key'lerinizi girin
```

#### Frontend

```bash
cd Frontend
cp .env.example .env
# .env dosyasını düzenleyin
```

#### n8n Workflows

n8n workflow'ları için detaylı kurulum talimatları:
```bash
cd n8n-workflows
# README.md dosyasını okuyun
```

### 3. Backend Kurulumu

```bash
cd SmartShopper.Api
dotnet restore
dotnet ef database update
dotnet run
```

API şu adreste çalışacak: `https://localhost:7013`

### 4. Frontend Kurulumu

```bash
cd Frontend
npm install
npm run dev
```

Frontend şu adreste çalışacak: `http://localhost:5173`

### 5. n8n Kurulumu (Opsiyonel)

Telegram bot için n8n kurulumu:

```bash
# Docker ile
docker run -it --rm --name n8n -p 5678:5678 -v ~/.n8n:/home/node/.n8n n8nio/n8n

# Veya npm ile
npm install n8n -g
n8n start
```

Detaylı talimatlar için `n8n-workflows/README.md` dosyasına bakın.

## 📚 API Endpoints

### Tarif Endpoints
- `POST /api/recipes/generate` - Tarif oluştur
- `POST /api/recipes/generate-with-prices` - Fiyatlarla birlikte tarif oluştur
- `GET /api/recipes/{userId}` - Kullanıcının tarifleri

### Ürün Endpoints
- `POST /api/products/verify` - Ürün önerilerini doğrula
- `POST /api/products/compare-prices` - Fiyat karşılaştır

### Debug Endpoints
- `GET /api/ai/debug/pipeline/{requestId}` - Pipeline loglarını görüntüle
- `GET /api/ai/debug/stats` - İstatistikleri görüntüle
- `GET /api/ai/debug/test-openai` - OpenAI servisini test et

## 🏗️ Mimari

### Dual-Model Verification Pipeline

```
User Request
    ↓
[Groq - Fast Generation]
    ↓
[OpenAI/Gemini - Validation]
    ↓
[Cache Layer]
    ↓
Response
```

1. **Groq (Llama 3.1)**: Hızlı draft oluşturma (~2-3 saniye)
2. **OpenAI/Gemini**: Doğrulama ve düzeltme (~3-5 saniye)
3. **Cache**: Tekrar eden istekler için hızlı yanıt

## 🧪 Test

```bash
cd SmartShopper.Api.Tests
dotnet test
```

## 📦 Docker ile Çalıştırma

```bash
docker-compose up -d
```

## 🔒 Güvenlik

- API key'ler environment variables'da saklanır
- Firebase Service Account key'i `.gitignore`'da
- Rate limiting middleware
- Input validation
- Output sanitization

## 📝 Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

## 📄 Bitirme Projesi Raporu

Bu proje bir bitirme projesi olarak geliştirilmiştir. Detaylı proje raporu ve dokümantasyonu için:

**📥 [Bitirme Projesi Raporu (PDF)](https://drive.google.com/file/d/1bn0Zjc2blpM3_-igfouS3VjiLXp-jZBi/view?usp=sharing)**

**Rapor İçeriği:**
- Proje tanımı ve amaç
- Sistem mimarisi ve tasarım
- Kullanılan teknolojiler ve araçlar
- Dual-Model AI verification sistemi
- Uygulama detayları ve kod örnekleri
- Test sonuçları ve performans analizi
- Sonuç ve değerlendirme
- Gelecek geliştirmeler

## � Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit edin (`git commit -m 'Add amazing feature'`)
4. Push edin (`git push origin feature/amazing-feature`)
5. Pull Request açın

## 📧 İletişim

Proje Sahibi - [@Abdulhadialayoub](https://github.com/Abdulhadialayoub)

Proje Linki: [https://github.com/Abdulhadialayoub/Smart-Shopping-List-Recommender](https://github.com/Abdulhadialayoub/Smart-Shopping-List-Recommender)
