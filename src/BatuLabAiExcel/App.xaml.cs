using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using Serilog;
using BatuLabAiExcel.ViewModels;
using BatuLabAiExcel.Services;
using BatuLabAiExcel.Views;
using BatuLabAiExcel.Data;

namespace BatuLabAiExcel;

/// <summary>
/// WPF Application class with DI integration and license validation
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    public IServiceProvider ServiceProvider => _host?.Services ?? throw new InvalidOperationException("Services not initialized");

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            // Build host
            _host = Program.CreateHostBuilder(Environment.GetCommandLineArgs()).Build();

            // Initialize database
            await InitializeDatabaseAsync();

            // Perform startup validation to determine which window to show
            var validationResult = await PerformStartupValidationAsync();
            
            if (validationResult.ShowMain)
            {
                // User is authenticated with valid license - show main window
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            else if (validationResult.ShowSubscription)
            {
                // User is authenticated but no valid license - show subscription window
                var subscriptionWindow = ServiceProvider.GetRequiredService<SubscriptionWindow>();
                MainWindow = subscriptionWindow;
                subscriptionWindow.Show();
            }
            else
            {
                // User not authenticated or session expired - show login window
                var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
                MainWindow = loginWindow;
                loginWindow.Show();
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed");
            MessageBox.Show($"Failed to start application: {ex.Message}", 
                          "Startup Error", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
            Shutdown();
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Apply migrations if auto-migration is enabled
            var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var enableAutoMigration = configuration.GetValue<bool>("Database:EnableAutoMigration");
            
            if (enableAutoMigration)
            {
                await context.Database.MigrateAsync();
                Log.Information("Database migrations applied successfully");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database initialization failed");
            throw;
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
            
            // User is authenticated, now check license status
            var licenseValidation = await licenseService.ValidateLicenseAsync(currentUser.Id);
            
            if (licenseValidation.IsValid && licenseValidation.License != null)
            {
                Log.Information("User {Email} has valid {LicenseType} license - showing main window", 
                               currentUser.Email, licenseValidation.License.Type);
                return new StartupValidationResult { ShowMain = true };
            }
            else
            {
                Log.Information("User {Email} has no valid license - showing subscription window", currentUser.Email);
                return new StartupValidationResult { ShowSubscription = true };
            }
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