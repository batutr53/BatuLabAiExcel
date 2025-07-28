# EMERGENCY BUILD FIX - Antivirus DLL Blocking Solution
Write-Host "=== EMERGENCY BUILD FIX ===" -ForegroundColor Red
Write-Host "Antivirus is blocking DLL files during build" -ForegroundColor Yellow

# Check admin
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    Write-Host "ADMINISTRATOR YETKILERİ GEREKLİ!" -ForegroundColor Red
    Write-Host "Bu script'i Administrator olarak çalıştırın" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "`n=== 1. REAL-TIME PROTECTION KAPATILIYOR ===" -ForegroundColor Cyan
try {
    Set-MpPreference -DisableRealtimeMonitoring $true -ErrorAction Stop
    Write-Host "✓ Real-time monitoring devre dışı" -ForegroundColor Green
} catch {
    Write-Host "❌ Real-time monitoring kapatılamadı: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== 2. BUILD FOLDERS TERİZLENİYOR ===" -ForegroundColor Cyan
try {
    if (Test-Path "src\BatuLabAiExcel\bin") { Remove-Item "src\BatuLabAiExcel\bin" -Recurse -Force }
    if (Test-Path "src\BatuLabAiExcel\obj") { Remove-Item "src\BatuLabAiExcel\obj" -Recurse -Force }
    Write-Host "✓ Build klasörleri temizlendi" -ForegroundColor Green
} catch {
    Write-Host "❌ Build klasörleri temizlenemedi: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== 3. PROJECT BUILD EDİLİYOR ===" -ForegroundColor Cyan
try {
    $buildResult = dotnet build "src\BatuLabAiExcel" --configuration Release --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Build başarılı!" -ForegroundColor Green
    } else {
        Write-Host "❌ Build başarısız!" -ForegroundColor Red
        Write-Host $buildResult -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Build hatası: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== 4. DLL'LER KONTROL EDİLİYOR ===" -ForegroundColor Cyan
$dllPath = "src\BatuLabAiExcel\bin\Release\net9.0-windows\BatuLabAiExcel.dll"
if (Test-Path $dllPath) {
    Write-Host "✓ Ana DLL mevcut: $dllPath" -ForegroundColor Green
    $dllCount = (Get-ChildItem "src\BatuLabAiExcel\bin\Release\net9.0-windows\*.dll").Count
    Write-Host "✓ Toplam $dllCount DLL dosyası oluşturuldu" -ForegroundColor Green
} else {
    Write-Host "❌ Ana DLL bulunamadı!" -ForegroundColor Red
}

Write-Host "`n=== 5. REAL-TIME PROTECTION AÇILIYOR ===" -ForegroundColor Cyan
try {
    Set-MpPreference -DisableRealtimeMonitoring $false -ErrorAction Stop
    Write-Host "✓ Real-time monitoring tekrar aktif" -ForegroundColor Green
} catch {
    Write-Host "❌ Real-time monitoring açılamadı: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== İZLEM ===`n" -ForegroundColor Yellow
Write-Host "Eğer build başarısız olduysa:" -ForegroundColor White
Write-Host "1. Windows Defender'ı tamamen devre dışı bırakın" -ForegroundColor Gray
Write-Host "2. Başka antivirus kullanıyorsanız onu da kapatın" -ForegroundColor Gray
Write-Host "3. Bu script'i tekrar çalıştırın" -ForegroundColor Gray
Write-Host "4. Build tamamlandıktan sonra antivirüsü açın" -ForegroundColor Gray

Write-Host "`nBaşka sorun varsa MANUAL_ANTIVIRUS_FIX.md dosyasına bakın" -ForegroundColor Cyan
pause