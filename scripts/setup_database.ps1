# Office AI Database Setup Script
# This script sets up PostgreSQL database for Office AI - Batu Lab

param(
    [string]$DatabaseName = "office_ai_batulabdb",
    [string]$Username = "office_ai_user",
    [string]$Password = "your_secure_password",
    [string]$Host = "localhost",
    [int]$Port = 5432,
    [switch]$CreateDatabase = $false,
    [switch]$RunMigrations = $false
)

Write-Host "🗄️ Office AI Database Setup" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green

# Check if PostgreSQL is installed
try {
    $pgVersion = & psql --version 2>$null
    Write-Host "✅ PostgreSQL found: $pgVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ PostgreSQL not found. Please install PostgreSQL first." -ForegroundColor Red
    Write-Host "Download from: https://www.postgresql.org/download/" -ForegroundColor Yellow
    exit 1
}

# Check if .NET EF Core tools are installed
try {
    $efVersion = & dotnet ef --version 2>$null
    Write-Host "✅ EF Core Tools found: $efVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ EF Core Tools not found. Installing..." -ForegroundColor Yellow
    try {
        & dotnet tool install --global dotnet-ef
        Write-Host "✅ EF Core Tools installed successfully" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to install EF Core Tools" -ForegroundColor Red
        exit 1
    }
}

if ($CreateDatabase) {
    Write-Host "`n🔧 Creating Database and User..." -ForegroundColor Cyan
    
    # Create database setup SQL
    $sqlCommands = @"
-- Create user if not exists
DO `$`$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$Username') THEN
      CREATE USER $Username WITH PASSWORD '$Password';
   END IF;
END
`$`$;

-- Create database if not exists
SELECT 'CREATE DATABASE $DatabaseName OWNER $Username' 
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$DatabaseName')\gexec

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE $DatabaseName TO $Username;
ALTER USER $Username CREATEDB;
"@

    # Write SQL to temp file
    $tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
    $sqlCommands | Out-File -FilePath $tempSqlFile -Encoding UTF8
    
    try {
        # Run SQL commands as postgres superuser
        Write-Host "Running database creation commands..." -ForegroundColor Yellow
        & psql -h $Host -p $Port -U postgres -f $tempSqlFile
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Database and user created successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ Database creation failed" -ForegroundColor Red
            exit 1
        }
    } catch {
        Write-Host "❌ Error running database setup: $_" -ForegroundColor Red
        exit 1
    } finally {
        # Clean up temp file
        if (Test-Path $tempSqlFile) {
            Remove-Item $tempSqlFile -Force
        }
    }
}

# Test database connection
Write-Host "`n🔗 Testing Database Connection..." -ForegroundColor Cyan
$connectionString = "Host=$Host;Port=$Port;Database=$DatabaseName;Username=$Username;Password=$Password"

try {
    # Create test connection using .NET
    Add-Type -AssemblyName System.Data
    $connection = New-Object Npgsql.NpgsqlConnection($connectionString)
    $connection.Open()
    $connection.Close()
    Write-Host "✅ Database connection successful" -ForegroundColor Green
} catch {
    Write-Host "❌ Database connection failed: $_" -ForegroundColor Red
    Write-Host "Please check your PostgreSQL installation and credentials." -ForegroundColor Yellow
    exit 1
}

if ($RunMigrations) {
    Write-Host "`n🏗️ Running Database Migrations..." -ForegroundColor Cyan
    
    # Set connection string environment variable
    $env:ConnectionStrings__DefaultConnection = $connectionString
    
    try {
        # Change to project directory
        $projectPath = Join-Path $PSScriptRoot ".." "src" "BatuLabAiExcel"
        Push-Location $projectPath
        
        # Add initial migration if it doesn't exist
        if (!(Test-Path "Migrations")) {
            Write-Host "Creating initial migration..." -ForegroundColor Yellow
            & dotnet ef migrations add InitialCreate
        }
        
        # Update database
        Write-Host "Applying migrations..." -ForegroundColor Yellow
        & dotnet ef database update
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Database migrations completed successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ Database migration failed" -ForegroundColor Red
            exit 1
        }
    } catch {
        Write-Host "❌ Error running migrations: $_" -ForegroundColor Red
        exit 1
    } finally {
        Pop-Location
    }
}

# Update appsettings.json with connection string
Write-Host "`n⚙️ Updating Configuration..." -ForegroundColor Cyan
try {
    $appSettingsPath = Join-Path $PSScriptRoot ".." "src" "BatuLabAiExcel" "appsettings.json"
    
    if (Test-Path $appSettingsPath) {
        $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
        $appSettings.Database.ConnectionString = $connectionString
        $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath
        Write-Host "✅ Configuration updated successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠️ appsettings.json not found at expected location" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error updating configuration: $_" -ForegroundColor Red
}

Write-Host "`n🎉 Database Setup Complete!" -ForegroundColor Green
Write-Host "Database: $DatabaseName" -ForegroundColor White
Write-Host "User: $Username" -ForegroundColor White
Write-Host "Host: $Host:$Port" -ForegroundColor White
Write-Host "`nConnection String:" -ForegroundColor White
Write-Host $connectionString -ForegroundColor Gray

Write-Host "`n📝 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Set up Stripe API keys in appsettings.json" -ForegroundColor White
Write-Host "2. Configure JWT secret key" -ForegroundColor White
Write-Host "3. Run the application: dotnet run" -ForegroundColor White