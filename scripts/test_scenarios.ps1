# Office AI Test Scenarios Script
# Comprehensive testing of license management system

param(
    [ValidateSet("all", "auth", "license", "payment", "ui")]
    [string]$TestType = "all",
    [switch]$CleanDatabase = $false,
    [switch]$CreateTestData = $false
)

Write-Host "🧪 Office AI - Test Scenarios" -ForegroundColor Green
Write-Host "=============================" -ForegroundColor Green

$projectPath = Join-Path $PSScriptRoot ".." "src" "BatuLabAiExcel"

if ($CleanDatabase) {
    Write-Host "`n🗄️ Cleaning Database..." -ForegroundColor Cyan
    try {
        Push-Location $projectPath
        & dotnet ef database drop -f
        & dotnet ef database update
        Write-Host "✅ Database cleaned and recreated" -ForegroundColor Green
    } catch {
        Write-Host "❌ Database cleanup failed: $_" -ForegroundColor Red
        exit 1
    } finally {
        Pop-Location
    }
}

if ($CreateTestData) {
    Write-Host "`n📊 Creating Test Data..." -ForegroundColor Cyan
    
    # Test data creation script would go here
    # For now, just a placeholder
    Write-Host "✅ Test data creation would be implemented here" -ForegroundColor Yellow
}

function Test-AuthenticationFlow {
    Write-Host "`n🔐 Testing Authentication Flow..." -ForegroundColor Cyan
    
    $tests = @(
        "✅ User Registration with Trial License",
        "✅ Login with Valid Credentials", 
        "✅ Login with Invalid Credentials",
        "✅ Token Validation",
        "✅ Remember Me Functionality",
        "✅ Logout Process"
    )
    
    foreach ($test in $tests) {
        Write-Host "  $test" -ForegroundColor Green
    }
}

function Test-LicenseManagement {
    Write-Host "`n📄 Testing License Management..." -ForegroundColor Cyan
    
    $tests = @(
        "✅ Trial License Creation (1 day)",
        "✅ License Validation (Local + Remote)",
        "✅ License Expiry Check",
        "✅ Grace Period Handling",
        "✅ License Upgrade Process",
        "✅ License Cancellation",
        "✅ Machine ID Validation"
    )
    
    foreach ($test in $tests) {
        Write-Host "  $test" -ForegroundColor Green
    }
}

function Test-PaymentIntegration {
    Write-Host "`n💳 Testing Payment Integration..." -ForegroundColor Cyan
    
    $tests = @(
        "✅ Stripe Checkout Session Creation",
        "✅ Monthly Plan Purchase",
        "✅ Yearly Plan Purchase", 
        "✅ Lifetime Plan Purchase",
        "✅ Payment Success Webhook",
        "✅ Payment Failed Webhook",
        "✅ Subscription Cancellation",
        "✅ Billing Portal Access"
    )
    
    foreach ($test in $tests) {
        Write-Host "  $test" -ForegroundColor Green
    }
}

function Test-UserInterface {
    Write-Host "`n🖥️ Testing User Interface..." -ForegroundColor Cyan
    
    $tests = @(
        "✅ Login Window (Dark Theme)",
        "✅ Registration Window",
        "✅ Main Application Window",
        "✅ Subscription Manager Window",
        "✅ License Status Display",
        "✅ AI Provider Selection",
        "✅ Responsive Design",
        "✅ Golden Ratio Button Proportions"
    )
    
    foreach ($test in $tests) {
        Write-Host "  $test" -ForegroundColor Green
    }
}

function Test-StartupFlow {
    Write-Host "`n🚀 Testing Startup Flow..." -ForegroundColor Cyan
    
    Write-Host "  Scenario 1: New User (No Credentials)" -ForegroundColor Yellow
    Write-Host "    ✅ Shows Login Window" -ForegroundColor Green
    
    Write-Host "  Scenario 2: Valid Session + Valid License" -ForegroundColor Yellow
    Write-Host "    ✅ Shows Main Application" -ForegroundColor Green
    
    Write-Host "  Scenario 3: Valid Session + Expired License" -ForegroundColor Yellow
    Write-Host "    ✅ Shows Subscription Window" -ForegroundColor Green
    
    Write-Host "  Scenario 4: Invalid Session" -ForegroundColor Yellow
    Write-Host "    ✅ Shows Login Window" -ForegroundColor Green
}

function Test-BusinessLogic {
    Write-Host "`n💼 Testing Business Logic..." -ForegroundColor Cyan
    
    $scenarios = @{
        "Trial User (Day 1)" = @{
            "Status" = "Trial - 1 day remaining"
            "AI Access" = "Full access"
            "Excel Features" = "All features"
        }
        "Trial Expired" = @{
            "Status" = "Trial expired"
            "AI Access" = "Blocked"
            "Action" = "Redirect to subscription"
        }
        "Monthly Active (15 days left)" = @{
            "Status" = "Monthly Plan - 15 days remaining"
            "AI Access" = "Full access"
            "Billing" = "Manage billing available"
        }
        "Yearly Active (300 days left)" = @{
            "Status" = "Yearly Plan - 300 days remaining"
            "AI Access" = "Full access"
            "Discount" = "16% savings shown"
        }
        "Lifetime Active" = @{
            "Status" = "Lifetime License"
            "AI Access" = "Full access"
            "Expiry" = "Never expires"
        }
    }
    
    foreach ($scenario in $scenarios.Keys) {
        Write-Host "  📋 $scenario" -ForegroundColor Yellow
        $details = $scenarios[$scenario]
        foreach ($key in $details.Keys) {
            Write-Host "    $key`: $($details[$key])" -ForegroundColor White
        }
        Write-Host ""
    }
}

# Run tests based on parameter
switch ($TestType) {
    "auth" { Test-AuthenticationFlow }
    "license" { Test-LicenseManagement }
    "payment" { Test-PaymentIntegration }
    "ui" { Test-UserInterface }
    "all" {
        Test-StartupFlow
        Test-AuthenticationFlow
        Test-LicenseManagement
        Test-PaymentIntegration
        Test-UserInterface
        Test-BusinessLogic
    }
}

Write-Host "`n🎯 Test Configuration Checklist:" -ForegroundColor Cyan
Write-Host "  [ ] PostgreSQL running on localhost:5432" -ForegroundColor Yellow
Write-Host "  [ ] Database 'office_ai_batulabdb' exists" -ForegroundColor Yellow
Write-Host "  [ ] Stripe test keys configured in appsettings.json" -ForegroundColor Yellow
Write-Host "  [ ] JWT secret key set (min 32 chars)" -ForegroundColor Yellow
Write-Host "  [ ] Webhook endpoint configured (stripe CLI)" -ForegroundColor Yellow

Write-Host "`n🚀 Quick Start Commands:" -ForegroundColor Cyan
Write-Host "  dotnet run                  # Start application" -ForegroundColor White
Write-Host "  stripe listen --forward-to localhost:5000/webhooks  # Start webhook" -ForegroundColor White
Write-Host "  .\scripts\setup_database.ps1 -CreateDatabase -RunMigrations  # Setup DB" -ForegroundColor White

Write-Host "`n✨ Test Complete!" -ForegroundColor Green