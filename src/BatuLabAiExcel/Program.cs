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
                services.Configure<AppConfiguration.AiProviderSettings>(context.Configuration.GetSection("AiProvider"));
                services.Configure<AppConfiguration.McpSettings>(context.Configuration.GetSection("Mcp"));

                // HTTP Clients
                services.AddHttpClient<IClaudeService, ClaudeService>();
                services.AddHttpClient<IGeminiService, GeminiService>();
                services.AddHttpClient<IGroqService, GroqService>();

                // AI Services
                services.AddSingleton<IClaudeService, ClaudeService>();
                services.AddSingleton<IGeminiService, GeminiService>();
                services.AddSingleton<IGroqService, GroqService>();
                
                // AI Provider Factory
                services.AddSingleton<IAiServiceFactory, AiServiceFactory>();
                
                services.AddSingleton<ClaudeAiService>();
                services.AddSingleton<GeminiAiService>();
                services.AddSingleton<GroqAiService>();

                // MCP and Chat Services
                services.AddSingleton<IChatOrchestrator, ChatOrchestrator>();
                services.AddSingleton<IMcpClient, McpClient>();
                
                // ViewModels
                services.AddTransient<MainViewModel>();
                
                // Infrastructure
                services.AddSingleton<ProcessHelper>();
            });

}