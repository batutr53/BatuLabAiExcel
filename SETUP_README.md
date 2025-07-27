# Office AI - Batu Lab | Lisans Yönetim Sistemi

Kapsamlı bir lisans yönetim sistemi ile güçlendirilmiş WPF Excel AI uygulaması.

## 🎯 Özellikler

### 🔐 Kimlik Doğrulama & Güvenlik
- **Kullanıcı Kaydı**: Otomatik 1 günlük deneme lisansı
- **Güvenli Giriş**: JWT token tabanlı kimlik doğrulama
- **Şifre Koruma**: BCrypt hash koruması
- **Güvenli Depolama**: Windows Credential Manager entegrasyonu
- **Oturum Yönetimi**: "Beni Hatırla" seçeneği

### 💳 Abonelik & Ödeme
- **Stripe Entegrasyonu**: Güvenli ödeme işleme
- **Esnek Planlar**: Aylık, Yıllık, Lifetime lisanslar
- **Otomatik Yenileme**: Subscription webhook'ları
- **Güvenli Checkout**: Stripe Checkout Session
- **Fatura Yönetimi**: Billing Portal entegrasyonu

### 📊 Lisans Yönetimi
- **Dinamik Doğrulama**: Yerel + uzak doğrulama
- **Süre Takibi**: Gerçek zamanlı kalan gün hesaplama
- **Otomatik Güncelleme**: Süresi dolan lisansları güncelleme
- **Grace Period**: 3 günlük ek süre desteği
- **Machine ID**: Donanım tabanlı lisans bağlama

### 🖥️ Modern UI/UX
- **Dark Theme**: 2025 standartlarında koyu tema
- **Golden Ratio**: Matematiksel oran uyumu
- **Responsive Design**: Ekran boyutuna uyum
- **Card-based Layout**: Modern kart tasarımı
- **Custom Controls**: Özel WPF kontrolleri

## 🏗️ Mimari Tasarım

### Katman Yapısı
```
┌─────────────────────┐
│   Presentation      │  ← WPF Views/ViewModels (MVVM)
├─────────────────────┤
│   Service Layer     │  ← Authentication, License, Payment
├─────────────────────┤
│   Data Layer        │  ← Entity Framework Core + PostgreSQL
├─────────────────────┤
│   External APIs     │  ← Stripe Payment, Claude AI
└─────────────────────┘
```

### Güvenlik Katmanları
1. **JWT Authentication**: Stateless token doğrulama
2. **License Validation**: Çift katmanlı (local + remote) doğrulama
3. **Secure Storage**: DPAPI şifreleme
4. **Rate Limiting**: API abuse koruması
5. **Input Validation**: XSS/Injection koruması

## 🛠️ Kurulum

### Ön Gereksinimler
- **.NET 9 SDK**
- **PostgreSQL 13+**
- **Visual Studio 2022** veya **VS Code**
- **Stripe Account** (Test/Production)

### 1. Veritabanı Kurulumu
```powershell
# PostgreSQL kurulumu ve veritabanı oluşturma
.\scripts\setup_database.ps1 -CreateDatabase -RunMigrations
```

### 2. Yapılandırma
```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Database=office_ai_batulabdb;Username=office_ai_user;Password=your_secure_password"
  },
  "Authentication": {
    "JwtSecretKey": "your-super-secret-jwt-key-change-in-production-min-32-chars"
  },
  "Stripe": {
    "SecretKey": "sk_test_your_stripe_secret_key",
    "PublishableKey": "pk_test_your_stripe_publishable_key",
    "WebhookSecret": "whsec_your_webhook_secret",
    "MonthlyPriceId": "price_monthly_plan_id",
    "YearlyPriceId": "price_yearly_plan_id",
    "LifetimePriceId": "price_lifetime_plan_id"
  }
}
```

### 3. Stripe Webhook Kurulumu
```bash
# Stripe CLI ile webhook dinleme
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

### 4. Uygulama Çalıştırma
```powershell
cd src\BatuLabAiExcel
dotnet restore
dotnet build
dotnet run
```

## 🧪 Test Senaryoları

### Senaryo 1: Yeni Kullanıcı Kaydı
```
1. Uygulama açılır → Login ekranı görülür
2. "Sign Up" tıklanır → Register ekranı açılır
3. Form doldurulur ve "Create Account" tıklanır
4. Otomatik 1 günlük trial lisans oluşturulur
5. Welcome email gönderilir (trial bilgisi ile)
6. Ana uygulama açılır
7. License Status: "Trial (1 Day) - 1 day remaining"
```

### Senaryo 2: Trial Süresi Bitmiş Kullanıcı
```
1. Uygulama açılır → Startup validation çalışır
2. Trial süresi dolmuş algılanır
3. Subscription ekranı otomatik açılır
4. "Choose Your Plan" görülür
5. Plan seçilir → Stripe Checkout açılır
6. Ödeme tamamlanır → Webhook tetiklenir
7. Lisans güncellenir → License key email gönderilir
8. Ana uygulama açılır
```

### Senaryo 3: Aktif Aylık Abonelik
```
1. Giriş yapılır → Startup validation geçer
2. Ana uygulama direkt açılır
3. License Status: "Monthly Plan - 25 days remaining"
4. AI özellikler kullanılabilir
5. Subscription manager açılır → "Manage Billing" görülür
```

### Senaryo 4: Ödeme Başarısız
```
1. Plan seçimi yapılır → Stripe Checkout açılır
2. Geçersiz kart bilgisi girilir
3. Stripe ödemeyi reddeder
4. Error message gösterilir
5. Kullanıcı trial ile devam eder
6. Subscription ekranına geri yönlendirilir
```

### Senaryo 5: Aktif Yıllık Abonelik
```
1. Yıllık plan satın alınır
2. License Status: "Yearly Plan - 365 days remaining"
3. Tüm premium özellikler açık
4. Billing portal üzerinden iptal edilebilir
```

### Senaryo 6: Lifetime Lisans
```
1. Lifetime plan satın alınır
2. License Status: "Lifetime License"
3. Süre kısıtlaması yok
4. Tüm premium özellikler permanent açık
```

## 🔧 Geliştirici Araçları

### Debug & Test
```powershell
# Veritabanını sıfırla
dotnet ef database drop -f
dotnet ef database update

# Test kullanıcısı oluştur
dotnet run -- --create-test-user

# Webhook test
stripe trigger payment_intent.succeeded
```

### Logging
```csharp
// Log dosyaları: logs/office-ai-batu-lab-{date}.log
Log.Information("User logged in: {Email}", email);
Log.Warning("License validation failed: {UserId}", userId);
Log.Error(ex, "Payment processing error");
```

## 🚀 Production Deployment

### 1. Güvenlik Kontrolleri
- [ ] JWT secret production'a değiştirildi
- [ ] PostgreSQL production veritabanı kuruldu
- [ ] Stripe production keys yapılandırıldı
- [ ] HTTPS certificate kuruldu
- [ ] Firewall kuralları ayarlandı

### 2. Performance Optimizasyonu
- [ ] Connection pooling etkinleştirildi
- [ ] Index'ler eklendi
- [ ] Caching stratejisi uygulandı
- [ ] Rate limiting konfigürasyonu

### 3. Monitoring
- [ ] Application Insights kuruldu
- [ ] Health checks eklendi
- [ ] Error tracking aktif
- [ ] Performance monitoring

## 📈 Genişletme Planları

### Kısa Vadeli
- [ ] Email doğrulama sistemi
- [ ] Password reset functionality
- [ ] Multi-factor authentication (MFA)
- [ ] Admin dashboard

### Orta Vadeli
- [ ] Team/Organization accounts
- [ ] Usage analytics
- [ ] API rate limiting per user
- [ ] Custom branding options

### Uzun Vadeli
- [ ] Multi-tenant architecture
- [ ] Marketplace integrations
- [ ] Enterprise SSO support
- [ ] White-label solutions

## 🔒 Güvenlik Önlemleri

### Implemented
✅ **Password Hashing**: BCrypt with salt  
✅ **JWT Tokens**: Secure stateless authentication  
✅ **HTTPS Only**: TLS 1.2+ enforcement  
✅ **Input Validation**: XSS/SQL injection protection  
✅ **Rate Limiting**: Brute force protection  
✅ **Secure Storage**: Windows DPAPI encryption  

### Recommended
- [ ] **WAF**: Web Application Firewall
- [ ] **DDoS Protection**: CloudFlare/AWS Shield
- [ ] **Security Headers**: HSTS, CSP, X-Frame-Options
- [ ] **Vulnerability Scanning**: Regular OWASP assessments
- [ ] **Backup Strategy**: Encrypted database backups

## 📞 Destek & İletişim

- **Teknik Destek**: support@batulab.com
- **Billing Issues**: billing@batulab.com
- **Bug Reports**: GitHub Issues
- **Feature Requests**: GitHub Discussions

---

© 2025 Batu Lab. Tüm hakları saklıdır.