# Stripe Webhook Test Script
# This script simulates Stripe webhook events for testing PaymentService

param(
    [string]$EventType = "checkout.session.completed",
    [string]$UserId = "12345678-1234-1234-1234-123456789012",
    [string]$LicenseType = "Monthly"
)

Write-Host "🔔 Stripe Webhook Test Script" -ForegroundColor Cyan
Write-Host "Testing PaymentService webhook handling..." -ForegroundColor Yellow

# Simulated webhook payloads
$checkoutSessionCompleted = @{
    "id" = "evt_test_webhook"
    "object" = "event"
    "api_version" = "2020-08-27"
    "created" = [System.DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    "data" = @{
        "object" = @{
            "id" = "cs_test_session_123"
            "object" = "checkout.session"
            "payment_status" = "paid"
            "amount_total" = 2999
            "currency" = "usd"
            "metadata" = @{
                "user_id" = $UserId
                "license_type" = $LicenseType
            }
            "payment_intent" = "pi_test_payment_123"
            "subscription" = if ($LicenseType -ne "Lifetime") { "sub_test_123" } else { $null }
        }
    }
    "livemode" = $false
    "pending_webhooks" = 1
    "request" = @{
        "id" = "req_test_123"
        "idempotency_key" = $null
    }
    "type" = "checkout.session.completed"
} | ConvertTo-Json -Depth 10

$invoicePaymentSucceeded = @{
    "id" = "evt_test_webhook_invoice"
    "object" = "event"
    "api_version" = "2020-08-27"
    "created" = [System.DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    "data" = @{
        "object" = @{
            "id" = "in_test_invoice_123"
            "object" = "invoice"
            "subscription" = "sub_test_123"
            "customer" = "cus_test_123"
            "amount_paid" = 2999
            "currency" = "usd"
            "status" = "paid"
        }
    }
    "livemode" = $false
    "type" = "invoice.payment_succeeded"
} | ConvertTo-Json -Depth 10

Write-Host "📊 Event Details:" -ForegroundColor Green
Write-Host "- Event Type: $EventType"
Write-Host "- User ID: $UserId"
Write-Host "- License Type: $LicenseType"
Write-Host

# Display the webhook payload that would be sent
Write-Host "📦 Webhook Payload:" -ForegroundColor Magenta
switch ($EventType) {
    "checkout.session.completed" {
        Write-Host $checkoutSessionCompleted -ForegroundColor Gray
    }
    "invoice.payment_succeeded" {
        Write-Host $invoicePaymentSucceeded -ForegroundColor Gray
    }
    default {
        Write-Host "Unknown event type: $EventType" -ForegroundColor Red
        exit 1
    }
}

Write-Host
Write-Host "🔧 Setup Instructions:" -ForegroundColor Yellow
Write-Host "1. In Stripe Dashboard, go to Developers > Webhooks"
Write-Host "2. Click 'Add endpoint'"
Write-Host "3. For testing, use ngrok:"
Write-Host "   ngrok http 5000"
Write-Host "   Then use: https://your-ngrok-id.ngrok.io/api/webhooks/stripe"
Write-Host
Write-Host "4. Select these events:"
Write-Host "   ✅ checkout.session.completed"
Write-Host "   ✅ invoice.payment_succeeded"
Write-Host "   ✅ customer.subscription.deleted"
Write-Host "   ✅ invoice.payment_failed"
Write-Host
Write-Host "5. Copy the webhook signing secret to appsettings.json:"
Write-Host '   "WebhookSecret": "whsec_your_webhook_secret_here"'
Write-Host

Write-Host "✅ Test completed. Check PaymentService logs for webhook processing." -ForegroundColor Green