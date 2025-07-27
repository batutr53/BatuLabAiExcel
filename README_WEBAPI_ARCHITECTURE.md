# Office AI - Batu Lab Web API Architecture

Bu dokument, güvenlik amacıyla ayrılmış Web API mimarisi hakkında detaylı bilgi sağlar.

## 🏗️ Mimari Genel Bakış

### Güvenlik Problemi
WPF desktop uygulamaları crack/hack edilebilir ve `appsettings.json` dosyalarındaki hassas bilgiler (veritabanı bağlantı stringi, Stripe secret key'leri, JWT secret'ları) kolayca çalınabilir.

### Çözüm: Client-Server Ayrışması
Tüm hassas operasyonları (veritabanı erişimi, authentication, payment processing) ayrı bir Web API projesi üzerinden gerçekleştiriyoruz.

```
┌─────────────────┐    HTTPS/JWT    ┌─────────────────┐
│   WPF Desktop   │ ──────────────> │   Web API       │
│   Application   │                 │   (Secure)      │
│                 │                 │                 │
│ • UI Only       │                 │ • Database      │
│ • MCP Client    │                 │ • Authentication│
│ • Excel AI      │                 │ • Payments      │
│                 │                 │ • Licenses      │
└─────────────────┘                 └─────────────────┘
                                             │
                                             ▼
                                    ┌─────────────────┐
                                    │   PostgreSQL    │
                                    │   Database      │
                                    └─────────────────┘
```

## 📁 Proje Yapısı

```
batulabaiexcel/
├── src/
│   ├── BatuLabAiExcel/              # WPF Desktop App
│   │   ├── Services/
│   │   │   ├── IWebApiClient.cs     # Web API iletişim interface
│   │   │   ├── WebApiClient.cs      # HTTP client implementation
│   │   │   ├── WebApiAuthenticationService.cs
│   │   │   ├── WebApiLicenseService.cs
│   │   │   └── WebApiPaymentService.cs
│   │   └── appsettings.json         # Sadece Web API URL'i
│   │
│   └── BatuLabAiExcel.WebApi/       # Secure Web API
│       ├── Controllers/
│       │   ├── AuthController.cs    # Login/Register endpoints
│       │   ├── LicenseController.cs # License validation
│       │   ├── PaymentController.cs # Stripe operations
│       │   └── WebhookController.cs # Stripe webhooks
│       ├── Services/
│       ├── Middleware/
│       └── appsettings.json         # Tüm hassas bilgiler
│
├── scripts/
│   ├── deploy_webapi.ps1           # Web API deployment script
│   └── setup_production.ps1        # Production environment setup
│
└── README_WEBAPI_ARCHITECTURE.md   # Bu dosya
```

## 🔑 API Endpoints

### Authentication Endpoints
```
POST /api/auth/login
POST /api/auth/register
GET  /api/auth/me
POST /api/auth/validate
```

### License Endpoints
```
POST /api/license/validate
GET  /api/license/current
POST /api/license/extend-trial
POST /api/license/cancel-subscription
```

### Payment Endpoints
```
GET  /api/payment/plans
POST /api/payment/create-checkout
POST /api/payment/verify/{sessionId}
```

### Webhook Endpoints
```
POST /api/webhook/stripe
GET  /api/webhook/health
POST /api/webhook/test
```

## 🚀 Deployment

### 1. Web API Deployment

#### IIS ile Deployment:
```powershell
# 1. Web API'yi publish et
dotnet publish src/BatuLabAiExcel.WebApi -c Release -o publish/webapi

# 2. IIS site oluştur
New-WebSite -Name "OfficeAI-WebAPI" -Port 443 -PhysicalPath "C:\inetpub\wwwroot\officeai-api"

# 3. SSL certificate ekle
# 4. appsettings.json'da production değerleri ayarla
```

#### Docker ile Deployment:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY publish/webapi .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "BatuLabAiExcel.WebApi.dll"]
```

### 2. WPF Application Configuration

WPF uygulamasında `appsettings.json`:
```json
{
  "WebApi": {
    "BaseUrl": "https://your-api-domain.com",
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "ValidateSslCertificate": true
  }
}
```

## 🔒 Güvenlik Özellikleri

### Web API Güvenlik
- **JWT Authentication**: Stateless, secure token authentication
- **Rate Limiting**: API abuse koruması
- **CORS Policy**: Cross-origin request kontrolü
- **Input Validation**: Tüm girdi validasyonu
- **Error Handling**: Güvenli hata mesajları
- **Request Logging**: Audit trail için detaylı loglama

### WPF Client Güvenlik
- **No Database Access**: Direkt veritabanı bağlantısı yok
- **No Sensitive Secrets**: Sadece Web API URL'i
- **JWT Token Management**: Token'lar memory'de tutulur
- **HTTPS Only**: Tüm API iletişimi encrypted

## 🔧 Development Environment

### 1. Web API'yi Çalıştırma
```powershell
cd src/BatuLabAiExcel.WebApi
dotnet run --launch-profile https
```
Web API varsayılan olarak `https://localhost:7001` adresinde çalışır.

### 2. WPF Uygulamasını Çalıştırma
```powershell
cd src/BatuLabAiExcel
dotnet run
```

### 3. Database Migration
```powershell
cd src/BatuLabAiExcel.WebApi
dotnet ef database update
```

## 📊 Monitoring & Logging

### Web API Logs
- **Serilog** ile structured logging
- **File logging**: `logs/webapi-.log`
- **Console logging**: Development ortamında
- **Request/Response logging**: Performance monitoring

### Performance Monitoring
- Response time tracking
- Rate limit monitoring
- Error rate tracking
- Database query performance

## 🧪 Testing

### API Testing
```powershell
# Health check
curl https://localhost:7001/api/webhook/health

# Login test
curl -X POST https://localhost:7001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"testpass"}'
```

### Integration Testing
```powershell
# WPF app'i test modunda çalıştır
dotnet run --environment Testing
```

## 🔄 Migration Plan

### Aşama 1: Web API Deployment
1. Web API'yi production sunucusuna deploy et
2. Database migration'ları çalıştır
3. Stripe webhook endpoint'lerini güncelle
4. SSL certificate konfigüre et

### Aşama 2: WPF App Update
1. WPF uygulamasında Web API client'ı aktifleştir
2. Eski database service'lerini devre dışı bırak
3. `appsettings.json`'dan hassas bilgileri kaldır
4. Production Web API URL'ini güncelle

### Aşama 3: Rollback Plan
- Eski veritabanı service'leri backup olarak tutuldu
- Configuration değişikliği ile eski sisteme dönüş mümkün
- Database backup'ları düzenli alınıyor

## 📋 Production Checklist

### Web API Production
- [ ] HTTPS certificate kuruldu
- [ ] Database connection string production'a güncellendi
- [ ] JWT secret production değeri ile değiştirildi
- [ ] Stripe live key'leri yapılandırıldı
- [ ] Email SMTP ayarları test edildi
- [ ] CORS policy production domain'leri içeriyor
- [ ] Rate limiting aktif
- [ ] Logging production seviyesinde
- [ ] Health check endpoint çalışıyor

### Stripe Webhook Configuration
- [ ] Webhook URL production API'ye güncellendi
- [ ] Webhook secret production değeri ile güncellendi
- [ ] Test webhook'ları başarılı
- [ ] Event delivery monitoring aktif

### WPF Application
- [ ] Web API URL production'a güncellendi
- [ ] SSL certificate validation aktif
- [ ] Error handling test edildi
- [ ] Offline senaryoları handle ediliyor
- [ ] User experience smooth

## 🆘 Troubleshooting

### Common Issues

#### Web API Connection Failed
```
Error: Unable to connect to Web API
```
**Çözüm**: 
- Web API service'inin çalıştığını kontrol et
- Firewall ayarlarını kontrol et
- SSL certificate'ı kontrol et

#### JWT Token Expired
```
Error: 401 Unauthorized
```
**Çözüm**: 
- Token expiry time'ını artır
- Refresh token implement et
- Auto-logout implement et

#### Database Connection Failed
```
Error: Database connection timeout
```
**Çözüm**: 
- Database server durumunu kontrol et
- Connection string'i kontrol et
- Connection pool ayarlarını optimize et

### Debug Commands
```powershell
# Web API logs
Get-Content logs/webapi-*.log | Select-String "ERROR"

# Health check
curl https://localhost:7001/api/webhook/health

# Database connection test
dotnet ef database update --dry-run
```

## 📞 Support

Bu mimari hakkında sorularınız için:
- GitHub Issues: Create new issue
- Documentation: README files
- Logs: Check application logs

---

**Not**: Bu mimari güvenlik ve scalability için tasarlandı. Production deployment öncesi tüm güvenlik kontrolleri yapılmalı.