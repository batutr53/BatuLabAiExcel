# 🛡️ Antivirus False Positive Çözüm Kılavuzu

## 🚨 Durum
**Office Ai - Batu Lab.** uygulaması bazı antivirüs programları tarafından yanlış olarak virus olarak algılanabilir. Bu **FALSE POSITIVE** bir durumdur.

## ✅ Bu Uygulama Güvenlidir Çünkü:

1. **Açık kaynak** - Tüm kod GitHub'da incelenebilir
2. **.NET 9 Framework** - Microsoft'un resmi teknolojisi
3. **Güvenli API entegrasyonu** - Sadece Claude AI ve Web API ile iletişim
4. **Şeffaf işlevsellik** - Excel automation ve AI integration
5. **Hiçbir kötü amaçlı kod** içermez

## 🔍 Neden False Positive Alıyor?

### Antivirüs Tetikleyicileri:
- **Process monitoring** (Excel penceresi bulma)
- **HTTP requests** (AI API çağrıları)
- **File operations** (Excel dosyası okuma/yazma)
- **Registry access** (ayar saklama)
- **Dynamic code execution** (MCP server başlatma)

### Normal İş Amaçlı Fonksiyonlar:
✅ Excel dosyalarını okur/yazar  
✅ AI servislerine API çağrıları yapar  
✅ Kullanıcı ayarlarını saklar  
✅ Python MCP server'ı başlatır  

## 🛠️ Çözüm Yöntemleri

### 1. **Windows Defender için:**
```powershell
# PowerShell'i Administrator olarak çalıştırın:
Add-MpPreference -ExclusionPath "E:\batulabaiexcel\src\BatuLabAiExcel\bin\Debug\net9.0-windows\"
Add-MpPreference -ExclusionProcess "BatuLabAiExcel.exe"
```

### 2. **Avast/AVG için:**
1. Avast/AVG arayüzünü açın
2. **Settings > Exceptions** bölümüne gidin
3. **Add Exception** tıklayın
4. Şu dosyayı ekleyin: `BatuLabAiExcel.exe`

### 3. **Norton için:**
1. Norton Security açın
2. **Settings > Antivirus > Scans and Risks > Exclusions/Low Risks**
3. **Configure** tıklayın
4. **Add** > **Files and Folders**
5. Uygulama klasörünü ekleyin

### 4. **Kaspersky için:**
1. Kaspersky açın
2. **Settings > Additional > Threats and Exclusions**
3. **Exclusions > Specify Trusted Applications**
4. **Add** > uygulama dosyasını seçin

### 5. **McAfee için:**
1. McAfee açın
2. **Virus and Spyware Protection > Excluded Files**
3. **Add File** > `BatuLabAiExcel.exe` seçin

## 🔒 Güvenlik Doğrulaması

### Dosya Hash Kontrolleri:
```bash
# SHA256 hash kontrolü yapabilirsiniz:
Get-FileHash "BatuLabAiExcel.exe" -Algorithm SHA256
```

### VirusTotal Kontrolü:
1. https://www.virustotal.com adresine gidin
2. Exe dosyasını upload edin
3. Sonuçları kontrol edin (çoğu antivirüs temiz olarak gösterecek)

## 📧 Destek

Eğer sorun devam ederse:
- **GitHub Issues:** Sorunları bildirin
- **Email:** Teknik destek için iletişime geçin

## ⚖️ Yasal Uyarı

Bu uygulama:
- Kişisel verileri toplamaz
- İnternet bağlantısını sadece AI servisleri için kullanır
- Sistem dosyalarını değiştirmez
- Arka planda gizli işlem yapmaz

**Bu bir iş uygulamasıdır ve tamamen güvenlidir.**