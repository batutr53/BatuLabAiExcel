using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Windows;
using System.Net.Http;
using BatuLabAiExcel.Services;
using BatuLabAiExcel.ViewModels;
using BatuLabAiExcel.Infrastructure;
using BatuLabAiExcel.Views;

namespace BatuLabAiExcel;

/// <summary>
/// Application entry point with Generic Host configuration
/// </summary>
public static class Program
{
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            })
            .UseSerilog((context, configuration) =>
            {
                var logConfig = context.Configuration.GetSection("Logging");
                var filePath = logConfig["File:Path"];
                
                configuration
                    .WriteTo.Console()
                    .WriteTo.File(
                        filePath ?? "logs/office-ai-batu-lab-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        fileSizeLimitBytes: 10 * 1024 * 1024);
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<AppConfiguration>(context.Configuration);
                services.Configure<AppConfiguration.ClaudeSettings>(context.Configuration.GetSection("Claude"));
                services.Configure<AppConfiguration.GeminiSettings>(context.Configuration.GetSection("Gemini"));
                services.Configure<AppConfiguration.GroqSettings>(context.Configuration.GetSection("Groq"));
                services.Configure<AppConfiguration.ClaudeCliSettings>(context.Configuration.GetSection("ClaudeCli"));
                services.Configure<AppConfiguration.GeminiCliSettings>(context.Configuration.GetSection("GeminiCli"));
                services.Configure<AppConfiguration.AiProviderSettings>(context.Configuration.GetSection("AiProvider"));
                services.Configure<AppConfiguration.McpSettings>(context.Configuration.GetSection("Mcp"));
                services.Configure<AppConfiguration.DesktopAutomationSettings>(context.Configuration.GetSection("DesktopAutomation"));
                services.Configure<AppConfiguration.WebApiSettings>(context.Configuration.GetSection("WebApi"));

                // Web API Client (replaces direct database access)
                services.AddHttpClient<WebApiClient>();
                services.AddScoped<IWebApiClient, WebApiClient>();

                // Secure API-based Services (no direct database access)
                services.AddScoped<IAuthenticationService, WebApiAuthenticationService>();
                services.AddScoped<ILicenseService, WebApiLicenseService>();
                services.AddScoped<IPaymentService, WebApiPaymentService>();
                services.AddSingleton<ISecureStorageService, SecureStorageService>();
                services.AddSingleton<IUserSettingsService, UserSettingsService>();

                // AI Services - Use scoped for HTTP clients
                services.AddHttpClient<ClaudeService>();
                services.AddHttpClient<GeminiService>();
                services.AddHttpClient<GroqService>();
                
                services.AddScoped<IClaudeService, ClaudeService>();
                services.AddScoped<IGeminiService, GeminiService>();
                services.AddScoped<IGroqService, GroqService>();
                services.AddSingleton<ClaudeCliService>();
                services.AddSingleton<GeminiCliService>();
                
                // AI Provider Factory
                services.AddSingleton<IAiServiceFactory, AiServiceFactory>();
                
                services.AddSingleton<ClaudeAiService>();
                services.AddSingleton<GeminiAiService>();
                services.AddSingleton<GroqAiService>();

                // Desktop Automation Services
                services.AddSingleton<WindowsAutomationHelper>();
                services.AddSingleton<ClaudeDesktopService>();
                services.AddSingleton<ChatGptDesktopService>();

                // MCP and Chat Services
                services.AddSingleton<IChatOrchestrator, ChatOrchestrator>();
                services.AddSingleton<IMcpClient, McpClient>();
                services.AddSingleton<IExcelDataProtectionService, ExcelDataProtectionService>();
                services.AddSingleton<IExcelProcessManager, ExcelProcessManager>();
                
                // ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<RegisterViewModel>();
                services.AddTransient<SubscriptionViewModel>();
                services.AddTransient<SettingsViewModel>();
                
                // Windows
                services.AddTransient<MainWindow>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<RegisterWindow>();
                services.AddTransient<SubscriptionWindow>();
                services.AddTransient<SettingsWindow>();
                
                // Infrastructure
                services.AddSingleton<ProcessHelper>();
            });

}