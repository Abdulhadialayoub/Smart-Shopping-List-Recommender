# n8n Workflows - Smart Shopper

Bu klasör Smart Shopper projesi için n8n workflow'larını içerir.

## 📋 Workflow'lar

### 1. telegram-bot-final.json
Ana Telegram bot workflow'u. Kullanıcılarla etkileşim, tarif önerileri ve fiyat karşılaştırma işlemlerini yönetir.

**Özellikler:**
- Telegram bot entegrasyonu
- Kullanıcı mesajlarını işleme
- Smart Shopper API ile iletişim
- Tarif önerileri
- Fiyat karşılaştırma

### 2. Diğer Workflow'lar
- `ai_shopping_assistant.json` - AI destekli alışveriş asistanı
- `cimri_price_tracker.json` - Cimri fiyat takibi
- `dynamic-price-agent.json` - Dinamik fiyat ajanı
- `enhanced_price_comparison.json` - Gelişmiş fiyat karşılaştırma
- `nutrition_tracker.json` - Besin değeri takibi
- `recipe-price-checker.json` - Tarif fiyat kontrolü
- `smart-shopping-assistant.json` - Akıllı alışveriş asistanı

## 🚀 Kurulum

### 1. n8n Kurulumu

#### Docker ile (Önerilen)
```bash
docker run -it --rm \
  --name n8n \
  -p 5678:5678 \
  -v ~/.n8n:/home/node/.n8n \
  n8nio/n8n
```

#### npm ile
```bash
npm install n8n -g
n8n start
```

### 2. Workflow'ları İçe Aktarma

1. n8n arayüzünü açın: `http://localhost:5678`
2. Sol menüden "Workflows" seçin
3. "Import from File" butonuna tıklayın
4. İstediğiniz workflow JSON dosyasını seçin
5. "Import" butonuna tıklayın

### 3. Telegram Bot Yapılandırması

#### Telegram Bot Token Alma

1. Telegram'da [@BotFather](https://t.me/botfather) ile konuşun
2. `/newbot` komutunu gönderin
3. Bot adını ve kullanıcı adını belirleyin
4. Aldığınız token'ı kaydedin

#### n8n'de Telegram Credential Ekleme

1. n8n'de "Credentials" menüsüne gidin
2. "New Credential" butonuna tıklayın
3. "Telegram" seçin
4. Bot token'ınızı girin
5. "Save" butonuna tıklayın

#### Webhook URL'ini Ayarlama

1. Workflow'u açın
2. Telegram Trigger node'unu seçin
3. "Webhook URL" kopyalayın
4. Bu URL'i Frontend `.env` dosyasına ekleyin:
   ```
   VITE_N8N_WEBHOOK_URL=your-webhook-url-here
   ```

### 4. API Endpoint'lerini Yapılandırma

Workflow içindeki HTTP Request node'larında API endpoint'lerini güncelleyin:

```
https://localhost:7013/api/recipes/generate
https://localhost:7013/api/products/compare-prices
```

Production'da:
```
https://your-domain.com/api/recipes/generate
https://your-domain.com/api/products/compare-prices
```

## 🔧 Yapılandırma

### Environment Variables

n8n için environment variables ayarlamak isterseniz:

```bash
# .env dosyası oluşturun
N8N_BASIC_AUTH_ACTIVE=true
N8N_BASIC_AUTH_USER=admin
N8N_BASIC_AUTH_PASSWORD=your-password

# Webhook URL
WEBHOOK_URL=https://your-domain.com

# Timezone
GENERIC_TIMEZONE=Europe/Istanbul
```

### Docker Compose ile Çalıştırma

```yaml
version: '3.8'

services:
  n8n:
    image: n8nio/n8n
    restart: always
    ports:
      - "5678:5678"
    environment:
      - N8N_BASIC_AUTH_ACTIVE=true
      - N8N_BASIC_AUTH_USER=admin
      - N8N_BASIC_AUTH_PASSWORD=your-password
      - WEBHOOK_URL=https://your-domain.com
      - GENERIC_TIMEZONE=Europe/Istanbul
    volumes:
      - ~/.n8n:/home/node/.n8n
```

## 📝 Workflow Kullanımı

### Telegram Bot Komutları

- `/start` - Botu başlat
- `/help` - Yardım menüsü
- `/recipe` - Tarif önerisi al
- `/prices` - Fiyat karşılaştır
- `/nutrition` - Besin değeri sorgula

### API Entegrasyonu

Workflow'lar Smart Shopper API ile şu endpoint'leri kullanır:

- `POST /api/recipes/generate` - Tarif oluştur
- `POST /api/recipes/generate-with-prices` - Fiyatlarla tarif
- `POST /api/products/compare-prices` - Fiyat karşılaştır
- `GET /api/nutrition/{foodName}` - Besin değeri

## 🐛 Hata Ayıklama

### Workflow Çalışmıyor

1. n8n loglarını kontrol edin:
   ```bash
   docker logs n8n
   ```

2. Webhook URL'inin doğru olduğundan emin olun
3. API endpoint'lerinin erişilebilir olduğunu kontrol edin
4. Telegram bot token'ının geçerli olduğunu doğrulayın

### Telegram Bot Yanıt Vermiyor

1. Bot token'ının doğru olduğunu kontrol edin
2. Webhook'un aktif olduğunu doğrulayın
3. n8n'in çalıştığından emin olun

## 📚 Kaynaklar

- [n8n Documentation](https://docs.n8n.io/)
- [Telegram Bot API](https://core.telegram.org/bots/api)
- [n8n Community](https://community.n8n.io/)

## 🔒 Güvenlik

- **ÖNEMLİ:** Bot token'larını asla GitHub'a commit etmeyin
- Workflow dosyalarındaki `<YOUR_BOT_TOKEN>` ve `<YOUR_CHAT_ID>` placeholder'larını kendi değerlerinizle değiştirin
- Production'da HTTPS kullanın
- n8n basic auth'u aktif edin
- Webhook URL'lerini güvenli tutun
- `.env` dosyalarını `.gitignore`'a ekleyin

## 📧 Destek

Sorularınız için:
- [GitHub Issues](https://github.com/Abdulhadialayoub/Smart-Shopping-List-Recommender/issues)
- [n8n Community Forum](https://community.n8n.io/)
