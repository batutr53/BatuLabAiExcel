using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using Serilog;
using BatuLabAiExcel.ViewModels;

namespace BatuLabAiExcel;

/// <summary>
/// WPF Application class with DI integration
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Configure dependency injection services
    /// </summary>
    /// <param name="serviceProvider">Service provider from Generic Host</param>
    public void ConfigureServices(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            var host = Program.CreateHostBuilder(Environment.GetCommandLineArgs()).Build();
            
            // Configure services
            ConfigureServices(host.Services);
            
            // Start the host
            _ = host.RunAsync();
            
            // Create and show main window
            var mainWindow = new MainWindow();
            var viewModel = _serviceProvider!.GetRequiredService<MainViewModel>();
            mainWindow.DataContext = viewModel;
            
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            MessageBox.Show($"Failed to start application: {ex.Message}", 
                          "Startup Error", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
            Shutdown();
        }
    }
}