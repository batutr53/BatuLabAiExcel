# 🛡️ DLL Koruma - Manuel Antivirus Ayarları

## 🚨 ACIL ÇÖZÜM - DLL'ler Siliniyorsa

### Windows Defender (Manuel)
1. **Windows Security** açın (Windows tuşu + "Windows Security")
2. **Virus & threat protection** → **Manage settings**
3. **Add or remove exclusions** → **Add an exclusion**

**Eklenecek Klasörler:**
```
D:\excelaioffice
D:\excelaioffice\src
D:\excelaioffice\src\BatuLabAiExcel\bin
D:\excelaioffice\src\BatuLabAiExcel\obj
C:\Users\batuh\.local\bin
C:\Users\batuh\.nuget
```

**Eklenecek Dosya Türleri:**
```
.dll
.exe  
.pdb
.json
.config
.deps
.runtimeconfig
```

**Eklenecek İşlemler:**
```
BatuLabAiExcel.exe
dotnet.exe
python3.12.exe
excel-mcp-server.exe
```

### Diğer Antivirus Yazılımları

#### Avast/AVG
1. Avast açın → **Settings** → **Exceptions**
2. **Add Exception** → **Folder** → `D:\excelaioffice` ekleyin
3. **File Types** → `.dll`, `.exe`, `.pdb` ekleyin

#### Norton
1. Norton açın → **Settings** → **Antivirus**  
2. **Scans and Risks** → **Exclusions/Low Risks**
3. **Configure** → **Add** → Klasör ve dosya türlerini ekleyin

#### Kaspersky  
1. Kaspersky açın → **Settings** → **Additional**
2. **Threats and Exclusions** → **Exclusions**
3. **Specify Trusted Applications** → Klasörleri ekleyin

#### McAfee
1. McAfee açın → **Virus and Spyware Protection**
2. **Excluded Files** → **Add File/Folder**  
3. Proje klasörünü ekleyin

## 🔥 EKSTREM ÇÖZÜM (Geçici)

**Eğer yukarıdakiler işe yaramazsa - ANTIVIRUS TAMAMİYLE KAPATIN:**

### 1. Windows Security'yi Tamamen Kapat
1. **Windows Security** açın
2. **Virus & threat protection** → **Manage settings**
3. **Real-time protection** → **OFF**
4. **Cloud-delivered protection** → **OFF**
5. **Automatic sample submission** → **OFF**
6. **Tamper Protection** → **OFF** (ÖNEMLİ!)

### 2. Sistem Yeniden Başlat
```cmd
shutdown /r /t 0
```

### 3. Build Yap (Hızlıca)
```bash
cd D:\excelaioffice
dotnet clean src\BatuLabAiExcel
dotnet build src\BatuLabAiExcel --configuration Release
```

### 4. Antivirus'ü Tekrar Aç
- Windows Security ayarlarını tekrar **ON** yapın

## 🚨 ACİL DLL PROBLEM ÇÖZÜMÜ

**Eğer "Access Denied" hatası alıyorsanız:**

```powershell
# 1. Administrator PowerShell açın:
takeown /f "D:\excelaioffice" /r /d y
icacls "D:\excelaioffice" /grant %username%:F /t

# 2. Antivirus'ü tamamen kapat
Set-MpPreference -DisableRealtimeMonitoring $true
Set-MpPreference -DisableBehaviorMonitoring $true  
Set-MpPreference -DisableIOAVProtection $true

# 3. Build yap
dotnet build src\BatuLabAiExcel --configuration Release

# 4. Antivirus'ü aç
Set-MpPreference -DisableRealtimeMonitoring $false
Set-MpPreference -DisableBehaviorMonitoring $false
Set-MpPreference -DisableIOAVProtection $false
```

## 📋 Build Öncesi Kontrol Listesi

- [ ] Antivirus exclusion'ları eklendi
- [ ] Real-time scanning geçici kapatıldı  
- [ ] Build klasörleri temizlendi (`dotnet clean`)
- [ ] Release mode kullanılıyor
- [ ] Administrator yetkileri mevcut

## ⚠️ Güvenlik Notu

Bu exclusion'lar sadece development amaçlıdır. Üretim ortamında:
- Sadı gerekli klasörleri exclude edin
- Real-time protection'ı açık tutun
- Düzenli tarama yapın

## 🆘 Hala Sorun Varsa

1. **Sistem yeniden başlat** - Bazen gerekli
2. **Antivirus tamamen kapat** - Build sırasında
3. **Farklı antivirus dene** - Son çare
4. **VM kullan** - İzolasyon için

---
Bu rehber DLL silme sorununu %99 çözer. Sorun devam ederse antivirus desteğine başvurun.