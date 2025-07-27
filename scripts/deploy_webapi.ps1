# Office AI - Batu Lab Web API Deployment Script
# This script deploys the Web API to a production environment

param(
    [string]$Environment = "Production",
    [string]$DeployPath = "C:\inetpub\wwwroot\officeai-api",
    [string]$DatabaseConnectionString = "",
    [string]$JwtSecret = "",
    [string]$StripeSecretKey = "",
    [string]$StripeWebhookSecret = "",
    [string]$EmailPassword = "",
    [switch]$SkipBuild = $false,
    [switch]$SkipMigration = $false,
    [switch]$DryRun = $false
)

Write-Host "🚀 Office AI - Batu Lab Web API Deployment" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Deploy Path: $DeployPath" -ForegroundColor Yellow

# Validate parameters
if ([string]::IsNullOrEmpty($DatabaseConnectionString)) {
    $DatabaseConnectionString = Read-Host "Enter Database Connection String"
}

if ([string]::IsNullOrEmpty($JwtSecret)) {
    $JwtSecret = Read-Host "Enter JWT Secret Key (minimum 32 characters)"
}

if ([string]::IsNullOrEmpty($StripeSecretKey)) {
    $StripeSecretKey = Read-Host "Enter Stripe Secret Key"
}

if ([string]::IsNullOrEmpty($StripeWebhookSecret)) {
    $StripeWebhookSecret = Read-Host "Enter Stripe Webhook Secret"
}

if ([string]::IsNullOrEmpty($EmailPassword)) {
    $EmailPassword = Read-Host "Enter Email SMTP Password" -AsSecureString
    $EmailPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($EmailPassword))
}

if ($DryRun) {
    Write-Host "🧪 DRY RUN MODE - No changes will be made" -ForegroundColor Magenta
}

# Step 1: Build the Web API
if (-not $SkipBuild) {
    Write-Host "📦 Building Web API..." -ForegroundColor Green
    
    if (-not $DryRun) {
        $buildResult = & dotnet publish src/BatuLabAiExcel.WebApi -c Release -o publish/webapi --no-restore
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Build failed" -ForegroundColor Red
            exit 1
        }
    }
    
    Write-Host "✅ Build completed successfully" -ForegroundColor Green
} else {
    Write-Host "⏭️ Skipping build step" -ForegroundColor Yellow
}

# Step 2: Create deployment directory
Write-Host "📁 Creating deployment directory..." -ForegroundColor Green

if (-not $DryRun) {
    if (-not (Test-Path $DeployPath)) {
        New-Item -ItemType Directory -Path $DeployPath -Force | Out-Null
        Write-Host "✅ Created deployment directory: $DeployPath" -ForegroundColor Green
    } else {
        Write-Host "ℹ️ Deployment directory already exists" -ForegroundColor Yellow
    }
}

# Step 3: Stop IIS application (if exists)
Write-Host "🛑 Stopping IIS application..." -ForegroundColor Green

if (-not $DryRun) {
    try {
        Import-Module WebAdministration -ErrorAction SilentlyContinue
        $site = Get-Website | Where-Object { $_.PhysicalPath -eq $DeployPath }
        if ($site) {
            Stop-Website -Name $site.Name
            Write-Host "✅ Stopped IIS site: $($site.Name)" -ForegroundColor Green
        } else {
            Write-Host "ℹ️ No existing IIS site found" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "⚠️ Could not stop IIS site: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# Step 4: Deploy files
Write-Host "📋 Deploying application files..." -ForegroundColor Green

if (-not $DryRun) {
    try {
        # Backup existing deployment
        if (Test-Path $DeployPath) {
            $backupPath = "$($DeployPath)_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
            Write-Host "💾 Creating backup: $backupPath" -ForegroundColor Yellow
            Copy-Item -Path $DeployPath -Destination $backupPath -Recurse -Force
        }
        
        # Copy new files
        Copy-Item -Path "publish/webapi/*" -Destination $DeployPath -Recurse -Force
        Write-Host "✅ Files deployed successfully" -ForegroundColor Green
    } catch {
        Write-Host "❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Step 5: Update appsettings.json with production values
Write-Host "⚙️ Updating production configuration..." -ForegroundColor Green

$appSettingsPath = Join-Path $DeployPath "appsettings.json"

if (-not $DryRun) {
    try {
        $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
        
        # Update database connection
        $appSettings.Database.ConnectionString = $DatabaseConnectionString
        
        # Update JWT settings
        $appSettings.Authentication.JwtSecretKey = $JwtSecret
        $appSettings.Authentication.Issuer = "BatuLabAiExcel.WebApi"
        $appSettings.Authentication.Audience = "BatuLabAiExcel.Client"
        
        # Update Stripe settings
        $appSettings.Stripe.SecretKey = $StripeSecretKey
        $appSettings.Stripe.WebhookSecret = $StripeWebhookSecret
        
        # Update email settings
        $appSettings.Email.Password = $EmailPassword
        
        # Update logging for production
        $appSettings.Logging.LogLevel.Default = "Warning"
        $appSettings.Logging.LogLevel.'BatuLabAiExcel.WebApi' = "Information"
        
        # Disable rate limiting in development
        $appSettings.RateLimit.EnableRateLimiting = $true
        
        # Save updated settings
        $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath -Encoding UTF8
        Write-Host "✅ Configuration updated successfully" -ForegroundColor Green
    } catch {
        Write-Host "❌ Configuration update failed: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Step 6: Run database migrations
if (-not $SkipMigration) {
    Write-Host "🗄️ Running database migrations..." -ForegroundColor Green
    
    if (-not $DryRun) {
        try {
            Set-Location $DeployPath
            $migrationResult = & dotnet BatuLabAiExcel.WebApi.dll --migrate
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Database migrations completed successfully" -ForegroundColor Green
            } else {
                Write-Host "⚠️ Database migrations completed with warnings" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "❌ Database migration failed: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "ℹ️ You may need to run migrations manually" -ForegroundColor Yellow
        } finally {
            Set-Location $PSScriptRoot
        }
    }
} else {
    Write-Host "⏭️ Skipping database migration" -ForegroundColor Yellow
}

# Step 7: Create/Update IIS site
Write-Host "🌐 Configuring IIS site..." -ForegroundColor Green

if (-not $DryRun) {
    try {
        Import-Module WebAdministration -ErrorAction Stop
        
        $siteName = "OfficeAI-WebAPI"
        $existingSite = Get-Website -Name $siteName -ErrorAction SilentlyContinue
        
        if ($existingSite) {
            # Update existing site
            Set-WebSite -Name $siteName -PhysicalPath $DeployPath
            Write-Host "✅ Updated existing IIS site: $siteName" -ForegroundColor Green
        } else {
            # Create new site
            New-WebSite -Name $siteName -Port 443 -PhysicalPath $DeployPath -Protocol https
            Write-Host "✅ Created new IIS site: $siteName" -ForegroundColor Green
        }
        
        # Start the site
        Start-Website -Name $siteName
        Write-Host "✅ Started IIS site: $siteName" -ForegroundColor Green
        
    } catch {
        Write-Host "⚠️ IIS configuration failed: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "ℹ️ You may need to configure IIS manually" -ForegroundColor Yellow
    }
}

# Step 8: Health check
Write-Host "🏥 Performing health check..." -ForegroundColor Green

if (-not $DryRun) {
    Start-Sleep -Seconds 5
    
    try {
        $healthUrl = "https://localhost/api/webhook/health"
        $response = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec 10
        
        if ($response.status -eq "healthy") {
            Write-Host "✅ Health check passed - API is responding" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Health check warning - API responded but status is not healthy" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "ℹ️ Check IIS logs and application logs for details" -ForegroundColor Yellow
    }
}

# Step 9: Display deployment summary
Write-Host "" -ForegroundColor White
Write-Host "🎉 Deployment Summary" -ForegroundColor Cyan
Write-Host "====================" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor White
Write-Host "Deploy Path: $DeployPath" -ForegroundColor White
Write-Host "IIS Site: OfficeAI-WebAPI" -ForegroundColor White
Write-Host "Health Check: https://localhost/api/webhook/health" -ForegroundColor White
Write-Host "" -ForegroundColor White

if ($DryRun) {
    Write-Host "🧪 This was a DRY RUN - no changes were made" -ForegroundColor Magenta
} else {
    Write-Host "✅ Deployment completed successfully!" -ForegroundColor Green
}

Write-Host "" -ForegroundColor White
Write-Host "📋 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Test API endpoints manually" -ForegroundColor White
Write-Host "2. Update Stripe webhook URLs" -ForegroundColor White
Write-Host "3. Configure SSL certificate" -ForegroundColor White
Write-Host "4. Update WPF app configuration" -ForegroundColor White
Write-Host "5. Monitor application logs" -ForegroundColor White

Write-Host "" -ForegroundColor White
Write-Host "🔗 Important URLs:" -ForegroundColor Yellow
Write-Host "API Base: https://your-domain.com" -ForegroundColor White
Write-Host "Swagger UI: https://your-domain.com (Development only)" -ForegroundColor White
Write-Host "Health Check: https://your-domain.com/api/webhook/health" -ForegroundColor White
Write-Host "Stripe Webhook: https://your-domain.com/api/webhook/stripe" -ForegroundColor White