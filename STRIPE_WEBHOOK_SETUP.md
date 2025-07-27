# 🔔 Stripe Webhook Kurulum Rehberi

Bu rehber, Office AI - Batu Lab uygulaması için Stripe webhook'larının nasıl kurulacağını açıklar.

## 🎯 Webhook Event'leri

PaymentService aşağıdaki event'leri handle eder:

### ✅ Dinlenmesi Gereken Event'ler:
```
checkout.session.completed     → Ödeme tamamlandığında
invoice.payment_succeeded      → Recurring ödeme başarılı olduğunda  
customer.subscription.deleted  → Abonelik iptal edildiğinde
invoice.payment_failed         → Ödeme başarısız olduğunda
```

## 🔧 Stripe Dashboard Kurulumu

### 1. Webhook Endpoint Oluşturma

1. **Stripe Dashboard**'a gidin: https://dashboard.stripe.com
2. **Developers** > **Webhooks** sekmesine gidin
3. **Add endpoint** butonuna tıklayın

### 2. Endpoint URL Ayarlama

**Test ortamı için:**
```
# ngrok kullanarak local tunnel
ngrok http 5000
# Çıkan URL: https://abc123.ngrok.io/api/webhooks/stripe
```

**Production ortamı için:**
```
https://your-domain.com/api/webhooks/stripe
```

### 3. Event Selection

Aşağıdaki event'leri seçin:

- ✅ `checkout.session.completed`
- ✅ `invoice.payment_succeeded`
- ✅ `customer.subscription.deleted`
- ✅ `invoice.payment_failed`

### 4. Webhook Secret

1. Webhook oluşturduktan sonra **Signing secret**'i kopyalayın
2. `appsettings.json`'a ekleyin:

```json
{
  "Stripe": {
    "WebhookSecret": "whsec_your_actual_webhook_secret_here"
  }
}
```

## 🏗️ Production Webhook Sunucusu

WPF uygulaması webhook alamaz. Production'da ayrı bir web API gereklidir:

### Option 1: ASP.NET Core Web API

```csharp
[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];
        
        // PaymentService'i kullanarak webhook'u işle
        var success = await _paymentService.HandleWebhookAsync(json, signature);
        return success ? Ok() : BadRequest();
    }
}
```

### Option 2: Azure Functions

```csharp
[FunctionName("StripeWebhook")]
public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "webhooks/stripe")] HttpRequest req)
{
    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    string signature = req.Headers["Stripe-Signature"];
    
    // Webhook işleme logic
    return new OkResult();
}
```

### Option 3: Minimal API

```csharp
app.MapPost("/api/webhooks/stripe", async (HttpContext context, IPaymentService paymentService) =>
{
    var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var signature = context.Request.Headers["Stripe-Signature"];
    
    var success = await paymentService.HandleWebhookAsync(json, signature);
    return success ? Results.Ok() : Results.BadRequest();
});
```

## 🧪 Test Senaryoları

### Test Script Kullanımı:

```powershell
# Checkout session completed test
.\scripts\test_stripe_webhook.ps1 -EventType "checkout.session.completed" -UserId "test-user-id" -LicenseType "Monthly"

# Invoice payment succeeded test  
.\scripts\test_stripe_webhook.ps1 -EventType "invoice.payment_succeeded" -UserId "test-user-id" -LicenseType "Yearly"
```

### Manuel Test:

1. Stripe Dashboard'da **Test mode**'u aktif edin
2. **Events** sekmesinde test event'leri oluşturun
3. **Send test webhook** ile endpoint'inizi test edin

## 🔒 Güvenlik

### Webhook Signature Verification

PaymentService otomatik olarak webhook signature'ını doğrular:

```csharp
var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
```

### IP Whitelist (Opsiyonel)

Stripe'ın webhook IP'lerini whitelist'e ekleyebilirsiniz:
- `54.187.174.169`
- `54.187.205.235`
- `54.187.216.72`
- `54.241.31.99`
- `54.241.31.102`
- `54.241.34.107`

## 📊 Monitoring & Logging

### Webhook Delivery Monitoring

1. Stripe Dashboard'da **Webhooks** > **Event delivery** sekmesi
2. Başarısız delivery'leri kontrol edin
3. Retry policy'sini ayarlayın

### Application Logging

PaymentService webhook işlemlerini loglar:

```csharp
_logger.LogInformation("Processing Stripe webhook: {EventType}", stripeEvent.Type);
_logger.LogInformation("Payment verified and license updated for user: {UserId}", userId);
```

## 🚀 Go Live Checklist

### Pre-Production:
- [ ] Test event'ler başarılı şekilde işleniyor
- [ ] Webhook signature verification çalışıyor
- [ ] Email notification'lar gönderiliyor
- [ ] Database güncellemeleri doğru yapılıyor

### Production:
- [ ] Live Stripe key'leri yapılandırıldı
- [ ] Production webhook endpoint URL set edildi
- [ ] HTTPS certificate geçerli
- [ ] Webhook secret production değeri ile güncellendi
- [ ] Monitoring ve alerting kuruldu

## 🔧 Troubleshooting

### Common Issues:

1. **Webhook signature verification failed**
   - Webhook secret'ın doğru olduğunu kontrol edin
   - Request body'nin modify edilmediğini doğrulayın

2. **Event not handled**
   - Event type'ın supported listede olduğunu kontrol edin
   - PaymentService switch statement'ını kontrol edin

3. **Database update failed**
   - User ID'nin valid olduğunu kontrol edin
   - License entity'nin doğru update edildiğini kontrol edin

### Debug Commands:

```powershell
# Webhook event'lerini test et
.\scripts\test_stripe_webhook.ps1

# Backend connectivity test
.\scripts\run_backend_check.ps1

# Log dosyalarını kontrol et
Get-Content logs\office-ai-batu-lab-*.log | Select-String "webhook"
```

---

💡 **Not**: Bu rehber PaymentService'deki mevcut webhook handling logic'i baz alır. Production'da webhook alabilmek için ayrı bir web service gereklidir.