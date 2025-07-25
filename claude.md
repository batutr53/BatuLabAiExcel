AMAÇ
Windows üzerinde çalışan bir **WPF (.NET 9) masaüstü uygulaması** geliştirmek istiyorum. Projenin **solution adı `batulabaiexcel`**, uygulamanın kullanıcıya görünen adı **“Office Ai - Batu Lab.”** olacak. Uygulama, kullanıcıdan gelen prompt’u **Claude Messages API**’ye gönderecek; Claude yanıtında gelen **tool_use** çağrılarını **MCP (Model Context Protocol)** konuşan **excel-mcp-server** (https://github.com/haris-musa/excel-mcp-server) üzerinden Excel’e yönlendirecek. MCP’den gelen sonuçlar **tool_result** olarak Claude’a geri dönecek ve final çıktı UI’da gösterilecek.

ÇOK ÖNEMLİ İSTEKLER
1. **Mimariyi tamamen sen tasarla**: Katmanlar, bağımlılıklar, sınıf ve namespace adları, paket seçimleri, error-handling, config, logging, test yaklaşımı… Hepsi için **neden** seçtiğini kısa gerekçelerle açıkla. (Örn. StreamJsonRpc mi yazacaksın, yoksa minimal bir JSON-RPC client mı? Neden?)
2. **excel-mcp-server reposunu oku** ve oradaki gerçek method adlarını, parametre şemalarını, initialize/handshake akışını birebir çıkar. (Eğer bir kısım belirsizse, mantıklı bir varsayım yap; README içinde “TODO: repodan teyit edilecek” diye işaretle, ama mümkün olduğunca doğru çıkarmaya çalış.)
3. **Tek bir cevapta eksiksiz bir proje ver**: 
   - **Solution + tüm .csproj’lar**
   - **Tüm C# kaynak kodları**
   - **appsettings.json**
   - **PowerShell scriptleri** (örn. `scripts/setup_mcp.ps1`, `scripts/run_backend_check.ps1`)
   - **README.md** (detaylı kurulum, çalıştırma, test, hata senaryoları, rate limit, güvenlik vs.)
   - **(İsteğe bağlı ama tercih edilir)** küçük bir **integration test** veya en azından bir **Console/Debug örneği** ile end-to-end senaryoyu kanıtla.
4. **Claude Messages API’nin tool_use / tool_result round-trip akışını eksiksiz uygula**. Kendi DTO’larını, JSON (de)serialization mantığını, error/timeout/retry stratejilerini yaz.
5. **UI başlığı, log dosya adları vb. gibi yerlerde uygulamanın görünen adını “Office Ai - Batu Lab.” olarak kullan**.
6. **En güncel .NET (9)**, **MVVM**, **Microsoft.Extensions.Hosting (Generic Host + DI)**, **Serilog** (dosya logu), **CommunityToolkit.Mvvm**, **System.Text.Json** kullan.
7. **appsettings.json** üzerinden ayarlanabilir yapı:
   - `Claude.ApiKey`, `Claude.Model`
   - `Mcp.PythonPath`, `Mcp.ServerScript`, `Mcp.WorkingDirectory`, `Mcp.TimeoutSeconds`
   - `Logging` (Serilog dosya yolu vs.)
8. **Güvenlik & Operasyonel detaylar**:
   - API key’lerin sızmaması (masking)
   - 429 / rate limit durumunda retry/backoff politikası
   - MCP tarafında timeout, cancel, yeniden başlatma stratejisi
9. **Kullanıcı Deneyimi**:
   - Basit bir chat UI: Prompt TextBox, Send butonu, Claude yanıtını gösteren alan, loading/busy state.
   - Hataları kullanıcıya uygun ve anlaşılır biçimde göster.
10. **Dağıtım ve kurulum**:
   - `scripts/setup_mcp.ps1`: `excel-mcp-server` repo’sunu klonlayıp venv kuran, `pip install -r requirements.txt` yapan script.
   - `scripts/run_backend_check.ps1`: MCP server’ı `--stdio` ile ayağa kaldırıp temel bir JSON-RPC request ile bağlantıyı doğrulayan script.
   - README’de Visual Studio ve `dotnet` CLI ile nasıl derlenip çalıştırılacağı, Python bağımlılıklarının nasıl paketleneceği (ister embeddable Python, ister kullanıcıya kurulum), production önerileri.
11. **Test edilebilir örnek senaryo**:
   - Prompt: “Sheet1!A1:C3 aralığını oku ve bana özetini ver.”
   - Beklenen akış: Claude → tool_use(`excel.read_range` benzeri gerçek method) → WPF MCP’ye yönlendirir → sonuç tool_result olarak Claude’a döner → Claude final metin üretir → UI’da gösterilir.
   - Bu round-trip’i README’de adım adım anlat; mümkünse integration-test benzeri bir örneği de sun.
12. **Kalite ve dokümantasyon**:
   - Public sınıflarda XML doc (özet seviyesinde)
   - Katmanlar arası bağımlılıklara dikkat (Domain UI/Infrastructure’a referans vermez)
   - CancellationToken her IO operasyonunda desteklensin
   - Hata mesajları ve logging belirgin, ayırt edilebilir
   - Kod bloklarını **dil etiketleri** ile ver (```csharp, ```json, ```powershell, vb.)

ÇIKTI FORMATIN (TEK CEVAPTA HEPSİ!)
1) **Mimari tasarım ve gerekçeler** (seçtiğin pattern’leri, paketleri, JSON-RPC stratejisini neden seçtiğini açıkla)
2) **Proje klasör ağacı (tree)**
3) **Tüm .csproj içerikleri**
4) **appsettings.json ve gerekiyorsa appsettings.Development.json**
5) **BÜTÜN C# kaynak kodları** (UI, ViewModels, Orchestrator, ClaudeService, McpClient, DTO’lar, Result tipleri, Program/App, vs.)
6) **PowerShell scriptleri** (`scripts/setup_mcp.ps1`, `scripts/run_backend_check.ps1`)
7) **README.md** (çok detaylı, kopyala-çalıştır netliğinde)
8) Eğer excel-mcp-server method & param şemalarını birebir çıkardıysan, bunların bir **EK** bölümünde tablo ve örnek JSON’larıyla listesi
9) **Örnek tool_use & tool_result payload’ları**
10) **TODO / Known Issues** bölümü: repodan teyit edilmesi gerekenler, gelecekte yapılacaklar

DİĞER NOTLAR
- **excel-mcp-server’ın gerçek methodlarını kullan** (örn. `excel/read_range`, `excel/write_range` vs. ne ise o). Yanlış isim bırakma. Emin değilsen geçici bir isim verme; repo’yu analiz ederek en doğru isimleri koy, zorunlu yerlerde TODO işaretle.
- Kodun derlenebilir olmasına dikkat et (usings, target frameworks, paket referansları).
- WPF için **tek bir MainWindow** ve basit ama temiz bir MVVM kurulumu yeterlidir.
- Çalışma zamanında MCP server process’i kapandığında yeniden başlatma veya kullanıcıya anlamlı uyarı verme stratejini yaz.
- Uygulama başlığında ve About bilgilerinde **“Office Ai - Batu Lab.”** ibaresini göster.

Şimdi tüm bu gereksinimleri uygulayarak, **tek mesajda** eksiksiz bir **çalışır başlangıç çözümü** (full code, script, README ile) üret.
