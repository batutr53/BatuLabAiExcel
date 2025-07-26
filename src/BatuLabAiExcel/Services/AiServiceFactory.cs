using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Factory implementation for creating AI services dynamically
/// </summary>
public class AiServiceFactory : IAiServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AppConfiguration.AiProviderSettings _providerSettings;
    private readonly ILogger<AiServiceFactory> _logger;

    public AiServiceFactory(
        IServiceProvider serviceProvider,
        IOptions<AppConfiguration.AiProviderSettings> providerSettings,
        ILogger<AiServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _providerSettings = providerSettings.Value;
        _logger = logger;
    }

    public IAiService GetAiService(string providerName)
    {
        try
        {
            return providerName?.ToLowerInvariant() switch
            {
                "claude" => _serviceProvider.GetRequiredService<ClaudeAiService>(),
                "gemini" => _serviceProvider.GetRequiredService<GeminiAiService>(),
                _ => GetDefaultAiService()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AI service for provider {Provider}", providerName);
            return GetDefaultAiService();
        }
    }

    public IAiService GetDefaultAiService()
    {
        try
        {
            return GetAiService(_providerSettings.DefaultProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default AI service, falling back to Claude");
            return _serviceProvider.GetRequiredService<ClaudeAiService>();
        }
    }

    public bool IsProviderAvailable(string providerName)
    {
        return providerName?.ToLowerInvariant() is "claude" or "gemini";
    }

    public IEnumerable<string> GetAvailableProviders()
    {
        return new[] { "Claude", "Gemini" };
    }
}