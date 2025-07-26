namespace BatuLabAiExcel.Services;

/// <summary>
/// Factory interface for creating AI services dynamically
/// </summary>
public interface IAiServiceFactory
{
    /// <summary>
    /// Get AI service by provider name
    /// </summary>
    IAiService GetAiService(string providerName);

    /// <summary>
    /// Get default AI service
    /// </summary>
    IAiService GetDefaultAiService();

    /// <summary>
    /// Check if provider is available
    /// </summary>
    bool IsProviderAvailable(string providerName);

    /// <summary>
    /// Get list of available providers
    /// </summary>
    IEnumerable<string> GetAvailableProviders();
}