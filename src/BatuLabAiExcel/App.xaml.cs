using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Windows;
using Serilog;
using BatuLabAiExcel.ViewModels;
using BatuLabAiExcel.Services;
using BatuLabAiExcel.Views;

namespace BatuLabAiExcel;

/// <summary>
/// WPF Application class with DI integration and license validation
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    public IServiceProvider ServiceProvider => _host?.Services ?? throw new InvalidOperationException("Services not initialized");

    protected override void OnStartup(StartupEventArgs e)
    {
        // Setup global exception handling
        this.DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        
        base.OnStartup(e);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled UI exception");
        MessageBox.Show($"Beklenmeyen hata: {e.Exception.Message}\n\nDetay: {e.Exception.InnerException?.Message}", 
                       "Hata - Office Ai - Batu Lab.", 
                       MessageBoxButton.OK, 
                       MessageBoxImage.Error);
        e.Handled = true;
        Shutdown();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log.Fatal(ex, "Unhandled domain exception");
            MessageBox.Show($"Kritik hata: {ex.Message}\n\nDetay: {ex.InnerException?.Message}", 
                           "Kritik Hata - Office Ai - Batu Lab.", 
                           MessageBoxButton.OK, 
                           MessageBoxImage.Error);
        }
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            Log.Information("Office AI - Batu Lab. starting up...");
            
            // Build host
            Log.Information("Building DI host...");
            _host = Program.CreateHostBuilder(Environment.GetCommandLineArgs()).Build();
            Log.Information("DI host built successfully");

            // Start with login window by default
            Log.Information("Creating LoginWindow...");
            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            Log.Information("LoginWindow created successfully");
            
            MainWindow = loginWindow;
            Log.Information("Showing LoginWindow...");
            loginWindow.Show();
            Log.Information("Application startup completed successfully");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Application startup failed: {ex.Message}";
            if (ex.InnerException != null)
                errorMsg += $"\nInner Exception: {ex.InnerException.Message}";
            
            errorMsg += $"\nStack Trace: {ex.StackTrace}";
            
            Log.Fatal(ex, "Application startup failed");
            MessageBox.Show($"Uygulama başlatılamadı: {ex.Message}\n\nDetaylı hata: {ex.InnerException?.Message}\n\nStack: {ex.StackTrace?.Substring(0, Math.Min(ex.StackTrace.Length, 500))}", 
                          "Başlatma Hatası - Office Ai - Batu Lab.", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
            Shutdown();
        }
    }


    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during application shutdown");
        }
        finally
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }

    private async Task<StartupValidationResult> PerformStartupValidationAsync()
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
            var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseService>();
            
            // Check if user has saved credentials and valid session
            var currentUser = await authService.GetCurrentUserAsync();
            
            if (currentUser == null)
            {
                Log.Information("No authenticated user found - showing login window");
                return new StartupValidationResult { ShowLogin = true };
            }
            
            // For now, since license validation is complex with Web API, show main window
            // In production, this would validate license through Web API
            Log.Information("User authenticated - showing main window");
            return new StartupValidationResult { ShowMain = true };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during startup validation - defaulting to login window");
            return new StartupValidationResult { ShowLogin = true };
        }
    }

    private class StartupValidationResult
    {
        public bool ShowLogin { get; set; }
        public bool ShowMain { get; set; }
        public bool ShowSubscription { get; set; }
    }
}