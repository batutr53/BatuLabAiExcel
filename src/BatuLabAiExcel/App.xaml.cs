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
    public static IServiceProvider StaticServiceProvider { get; private set; } = null!;

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

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            Log.Information("Office AI - Batu Lab. starting up...");
            
            // Build host
            Log.Information("Building DI host...");
            _host = Program.CreateHostBuilder(Environment.GetCommandLineArgs()).Build();
            StaticServiceProvider = _host.Services;
            Log.Information("DI host built successfully");

            // Perform startup validation to determine which window to show
            Log.Information("Performing startup validation...");
            var startupResult = await PerformStartupValidationAsync();
            
            Window windowToShow = startupResult.WindowToShow switch
            {
                WindowType.Main => ServiceProvider.GetRequiredService<MainWindow>(),
                WindowType.Subscription => ServiceProvider.GetRequiredService<Views.SubscriptionWindow>(),
                WindowType.Login or _ => ServiceProvider.GetRequiredService<Views.LoginWindow>()
            };
            
            Log.Information("Showing {WindowType} window...", startupResult.WindowToShow);
            MainWindow = windowToShow;
            windowToShow.Show();
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

    /// <summary>
    /// Perform startup validation to determine which window to show
    /// </summary>
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
                return new StartupValidationResult { WindowToShow = WindowType.Login };
            }
            
            // Validate license
            var licenseValidation = await licenseService.ValidateLicenseAsync(currentUser.Id);
            
            if (!licenseValidation.IsValid)
            {
                Log.Information("User has invalid/expired license - showing subscription window");
                return new StartupValidationResult { WindowToShow = WindowType.Subscription };
            }
            
            Log.Information("User authenticated with valid license - showing main window");
            return new StartupValidationResult { WindowToShow = WindowType.Main };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during startup validation - defaulting to login window");
            return new StartupValidationResult { WindowToShow = WindowType.Login };
        }
    }

    private class StartupValidationResult
    {
        public WindowType WindowToShow { get; set; } = WindowType.Login;
    }

    private enum WindowType
    {
        Login,
        Main,
        Subscription
    }
}