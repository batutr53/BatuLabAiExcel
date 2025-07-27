using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using BatuLabAiExcel.Models;
using BatuLabAiExcel.Services;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace BatuLabAiExcel.ViewModels;

/// <summary>
/// Settings view model for managing API keys and preferences
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IUserSettingsService _settingsService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private string _claudeApiKey = string.Empty;

    [ObservableProperty]
    private string _geminiApiKey = string.Empty;

    [ObservableProperty]
    private string _groqApiKey = string.Empty;

    [ObservableProperty]
    private string _selectedProvider = "Claude";

    [ObservableProperty]
    private bool _hasClaudeApiKey;

    [ObservableProperty]
    private bool _hasGeminiApiKey;

    [ObservableProperty]
    private bool _hasGroqApiKey;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isStatusVisible;

    public ObservableCollection<string> AvailableProviders { get; } = new()
    {
        "Claude", "Gemini", "Groq"
    };

    public SettingsViewModel(
        IUserSettingsService settingsService,
        ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _logger = logger;

        // Load existing settings
        _ = Task.Run(LoadSettingsAsync);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Saving settings...";
            IsStatusVisible = true;

            // Save API keys
            var tasks = new List<Task<Result>>();

            if (!string.IsNullOrWhiteSpace(ClaudeApiKey))
            {
                tasks.Add(_settingsService.SetApiKeyAsync("Claude", ClaudeApiKey));
            }

            if (!string.IsNullOrWhiteSpace(GeminiApiKey))
            {
                tasks.Add(_settingsService.SetApiKeyAsync("Gemini", GeminiApiKey));
            }

            if (!string.IsNullOrWhiteSpace(GroqApiKey))
            {
                tasks.Add(_settingsService.SetApiKeyAsync("Groq", GroqApiKey));
            }

            // Save default provider
            tasks.Add(_settingsService.SetDefaultProviderAsync(SelectedProvider));

            var results = await Task.WhenAll(tasks);
            var failedResults = results.Where(r => !r.IsSuccess).ToList();

            if (failedResults.Count > 0)
            {
                StatusMessage = $"Some settings failed to save: {string.Join(", ", failedResults.Select(r => r.Error))}";
                _logger.LogWarning("Failed to save some settings: {Errors}", string.Join(", ", failedResults.Select(r => r.Error)));
            }
            else
            {
                StatusMessage = "Settings saved successfully!";
                _logger.LogInformation("Settings saved successfully");
                
                // Update status indicators
                await LoadApiKeyStatusAsync();
            }

            // Hide status message after 3 seconds
            await Task.Delay(3000);
            IsStatusVisible = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving settings: {ex.Message}";
            _logger.LogError(ex, "Error saving settings");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TestApiKeyAsync(string provider)
    {
        try
        {
            IsBusy = true;
            StatusMessage = $"Testing {provider} API key...";
            IsStatusVisible = true;

            string? apiKey = provider switch
            {
                "Claude" => ClaudeApiKey,
                "Gemini" => GeminiApiKey,
                "Groq" => GroqApiKey,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                StatusMessage = $"Please enter {provider} API key first";
                return;
            }

            // Here you would test the API key by making a simple request
            // For now, just validate format
            if (IsValidApiKeyFormat(provider, apiKey))
            {
                StatusMessage = $"{provider} API key format looks valid";
            }
            else
            {
                StatusMessage = $"{provider} API key format appears invalid";
            }

            await Task.Delay(2000);
            IsStatusVisible = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error testing {provider} API key: {ex.Message}";
            _logger.LogError(ex, "Error testing API key for provider: {Provider}", provider);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ClearApiKeyAsync(string provider)
    {
        try
        {
            switch (provider)
            {
                case "Claude":
                    ClaudeApiKey = string.Empty;
                    break;
                case "Gemini":
                    GeminiApiKey = string.Empty;
                    break;
                case "Groq":
                    GroqApiKey = string.Empty;
                    break;
            }

            StatusMessage = $"{provider} API key cleared";
            IsStatusVisible = true;
            
            await Task.Delay(2000);
            IsStatusVisible = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing API key for provider: {Provider}", provider);
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            // Load default provider
            SelectedProvider = await _settingsService.GetDefaultProviderAsync();

            // Load API key status (don't load actual keys for security)
            await LoadApiKeyStatusAsync();

            _logger.LogInformation("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
        }
    }

    private async Task LoadApiKeyStatusAsync()
    {
        try
        {
            HasClaudeApiKey = await _settingsService.HasApiKeyAsync("Claude");
            HasGeminiApiKey = await _settingsService.HasApiKeyAsync("Gemini");
            HasGroqApiKey = await _settingsService.HasApiKeyAsync("Groq");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading API key status");
        }
    }

    private static bool IsValidApiKeyFormat(string provider, string apiKey)
    {
        return provider switch
        {
            "Claude" => apiKey.StartsWith("sk-ant-"),
            "Gemini" => apiKey.StartsWith("AIza") && apiKey.Length > 20,
            "Groq" => apiKey.StartsWith("gsk_") && apiKey.Length > 20,
            _ => !string.IsNullOrWhiteSpace(apiKey)
        };
    }
}