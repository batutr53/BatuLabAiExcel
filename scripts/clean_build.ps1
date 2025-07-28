# Clean build script with lock resolution
# Bu script lock sorunlarini cozup temiz build yapar

param(
    [string]$Configuration = "Debug",
    [switch]$Release = $false
)

if ($Release) {
    $Configuration = "Release"
}

Write-Host "Clean Build - Lock Resolution" -ForegroundColor Green
Write-Host "=============================" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor White

# Step 1: Kill locking processes
Write-Host "`n1. Locking processes sonlandiriliyor..." -ForegroundColor Cyan
try {
    & "$PSScriptRoot\kill_locking_processes.ps1"
    Start-Sleep -Seconds 2
} catch {
    Write-Warning "Process sonlandirma scripti calistirilirken hata: $($_.Exception.Message)"
}

# Step 2: Clean solution
Write-Host "`n2. Solution temizleniyor..." -ForegroundColor Cyan
try {
    Set-Location "D:\excelaioffice"
    
    # Clean using dotnet
    $cleanResult = dotnet clean --configuration $Configuration 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "dotnet clean: BASARILI" -ForegroundColor Green
    } else {
        Write-Warning "dotnet clean: HATALI"
        Write-Host $cleanResult -ForegroundColor Yellow
    }
    
    # Manual cleanup of problematic folders
    $foldersToClean = @(
        "src\BatuLabAiExcel\bin",
        "src\BatuLabAiExcel\obj"
    )
    
    foreach ($folder in $foldersToClean) {
        if (Test-Path $folder) {
            try {
                Remove-Item $folder -Recurse -Force -ErrorAction Stop
                Write-Host "Manuel temizlik: $folder" -ForegroundColor Green
            } catch {
                Write-Warning "Manuel temizlik basarisiz: $folder - $($_.Exception.Message)"
            }
        }
    }
    
} catch {
    Write-Error "Clean isleminde hata: $($_.Exception.Message)"
}

# Step 3: Wait a moment for file system
Write-Host "`n3. File system sync bekleniyor..." -ForegroundColor Cyan
Start-Sleep -Seconds 3

# Step 4: Restore packages
Write-Host "`n4. Package restore..." -ForegroundColor Cyan
try {
    $restoreResult = dotnet restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "dotnet restore: BASARILI" -ForegroundColor Green
    } else {
        Write-Warning "dotnet restore: HATALI"
        Write-Host $restoreResult -ForegroundColor Yellow
    }
} catch {
    Write-Error "Restore isleminde hata: $($_.Exception.Message)"
}

# Step 5: Build
Write-Host "`n5. Build islemi..." -ForegroundColor Cyan
try {
    $buildResult = dotnet build --configuration $Configuration --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "dotnet build: BASARILI" -ForegroundColor Green
        
        # Check if DLL was created
        $dllPath = "src\BatuLabAiExcel\bin\$Configuration\net9.0-windows\BatuLabAiExcel.dll"
        if (Test-Path $dllPath) {
            $dllInfo = Get-Item $dllPath
            Write-Host "DLL olusturuldu: $($dllInfo.Length) bytes, $($dllInfo.LastWriteTime)" -ForegroundColor Green
        } else {
            Write-Warning "DLL dosyasi bulunamadi: $dllPath"
        }
        
    } else {
        Write-Error "dotnet build: BASARISIZ"
        Write-Host $buildResult -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Error "Build isleminde hata: $($_.Exception.Message)"
    exit 1
}

Write-Host "`n=== Clean Build Tamamlandi ===" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host "Proje hazir!" -ForegroundColor Green