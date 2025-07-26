namespace BatuLabAiExcel.Services;

/// <summary>
/// Application configuration settings
/// </summary>
public class AppConfiguration
{
    public ClaudeSettings Claude { get; set; } = new();
    public GeminiSettings Gemini { get; set; } = new();
    public GroqSettings Groq { get; set; } = new();
    public AiProviderSettings AiProvider { get; set; } = new();
    public McpSettings Mcp { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public ApplicationSettings Application { get; set; } = new();

    public class ClaudeSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "claude-3-5-sonnet-20241022";
        public string BaseUrl { get; set; } = "https://api.anthropic.com";
        public string ApiVersion { get; set; } = "2023-06-01";
        public int MaxTokens { get; set; } = 4096;
        public int TimeoutSeconds { get; set; } = 120;
        public int RetryCount { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 2;
        public int RequestDelayMs { get; set; } = 2000;
        public double Temperature { get; set; } = 0.1;
        public double TopP { get; set; } = 0.9;
        public int TopK { get; set; } = 5;

        /// <summary>
        /// Get masked API key for logging
        /// </summary>
        public string GetMaskedApiKey()
        {
            if (string.IsNullOrEmpty(ApiKey) || ApiKey.Length < 8)
                return "***";
            
            return $"{ApiKey[..4]}***{ApiKey[^4..]}";
        }
    }

    public class GeminiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-2.5-flash";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
        public int MaxTokens { get; set; } = 2000;
        public int TimeoutSeconds { get; set; } = 120;
        public int RetryCount { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 2;
        public int RequestDelayMs { get; set; } = 1000;
        public double Temperature { get; set; } = 0.1;
        public double TopP { get; set; } = 0.9;
        public int TopK { get; set; } = 5;

        /// <summary>
        /// Get masked API key for logging
        /// </summary>
        public string GetMaskedApiKey()
        {
            if (string.IsNullOrEmpty(ApiKey) || ApiKey.Length < 8)
                return "***";
            
            return $"{ApiKey[..4]}***{ApiKey[^4..]}";
        }
    }

    public class GroqSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "llama-3.3-70b-versatile";
        public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
        public int MaxTokens { get; set; } = 2000;
        public int TimeoutSeconds { get; set; } = 120;
        public int RetryCount { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 2;
        public int RequestDelayMs { get; set; } = 500;
        public double Temperature { get; set; } = 0.1;
        public double TopP { get; set; } = 0.9;
        public int TopK { get; set; } = 5;

        /// <summary>
        /// Get masked API key for logging
        /// </summary>
        public string GetMaskedApiKey()
        {
            if (string.IsNullOrEmpty(ApiKey) || ApiKey.Length < 8)
                return "***";
            
            return $"{ApiKey[..4]}***{ApiKey[^4..]}";
        }
    }

    public class AiProviderSettings
    {
        public string DefaultProvider { get; set; } = "Claude";
        public bool AllowProviderSelection { get; set; } = true;
        
        public bool IsValidProvider(string provider)
        {
            return provider?.ToLowerInvariant() is "claude" or "gemini" or "groq" or "claude cli" or "claude desktop" or "chatgpt desktop";
        }
        
        public string GetValidProvider(string? provider = null)
        {
            if (!string.IsNullOrEmpty(provider) && IsValidProvider(provider))
                return char.ToUpper(provider[0]) + provider[1..].ToLowerInvariant();
                
            return DefaultProvider;
        }
    }

    public class McpSettings
    {
        public string PythonPath { get; set; } = "python";
        public string ServerScript { get; set; } = "python -m excel_mcp_server stdio";
        public string ServerScriptFallback { get; set; } = "uvx excel-mcp-server stdio";
        public string ServerScriptInstallFirst { get; set; } = "python -m pip install excel-mcp-server && python -m excel_mcp_server stdio";
        public string WorkingDirectory { get; set; } = "./excel_files";
        public int TimeoutSeconds { get; set; } = 30;
        public bool RestartOnFailure { get; set; } = true;
        public int MaxRestartAttempts { get; set; } = 3;
        public bool AutoInstall { get; set; } = true;
        public bool UseConfigFile { get; set; } = true;
    }

    public class LoggingSettings
    {
        public Dictionary<string, string> LogLevel { get; set; } = new();
        public FileSettings File { get; set; } = new();

        public class FileSettings
        {
            public string Path { get; set; } = "logs/office-ai-batu-lab-.log";
            public string RollingInterval { get; set; } = "Day";
            public int RetainedFileCountLimit { get; set; } = 7;
            public long FileSizeLimitBytes { get; set; } = 10 * 1024 * 1024; // 10MB
        }
    }

    public class ApplicationSettings
    {
        public string Title { get; set; } = "Office Ai - Batu Lab.";
        public string Version { get; set; } = "1.0.0";
        public bool EnableDebugMode { get; set; } = false;
    }

    public class DesktopAutomationSettings
    {
        public ClaudeDesktopSettings Claude { get; set; } = new();
        public ChatGptDesktopSettings ChatGPT { get; set; } = new();
        public GeneralDesktopSettings General { get; set; } = new();
    }

    public class ClaudeDesktopSettings
    {
        public bool Enabled { get; set; } = true;
        public string ProcessName { get; set; } = "claude";
        public string WindowTitle { get; set; } = "Claude";
        public string InputSelector { get; set; } = "#message-input";
        public string SendButtonSelector { get; set; } = "[aria-label='Send Message']";
        public string ResponseSelector { get; set; } = ".conversation-item:last-child .message-content";
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        public string LaunchCommand { get; set; } = "claude://";
        public string LaunchPath { get; set; } = "C:\\Users\\%USERNAME%\\AppData\\Local\\Claude\\Claude.exe";
    }

    public class ChatGptDesktopSettings
    {
        public bool Enabled { get; set; } = true;
        public string ProcessName { get; set; } = "ChatGPT";
        public string WindowTitle { get; set; } = "ChatGPT";
        public string InputSelector { get; set; } = "#prompt-textarea";
        public string SendButtonSelector { get; set; } = "[data-testid='send-button']";
        public string ResponseSelector { get; set; } = "[data-message-author-role='assistant']:last-child .markdown";
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        public string LaunchCommand { get; set; } = "chatgpt://";
        public string LaunchPath { get; set; } = "C:\\Users\\%USERNAME%\\AppData\\Local\\ChatGPT\\ChatGPT.exe";
    }

    public class GeneralDesktopSettings
    {
        public bool EnableAutoLaunch { get; set; } = true;
        public int WaitForAppStartup { get; set; } = 5000;
        public int MessageDelay { get; set; } = 1000;
        public int ResponsePollInterval { get; set; } = 500;
        public int MaxResponseWaitTime { get; set; } = 60000;
    }

    public class ClaudeCliSettings
    {
        public bool Enabled { get; set; } = true;
        public string ExecutablePath { get; set; } = "claude";
        public string WorkingDirectory { get; set; } = "./excel_files";
        public int TimeoutSeconds { get; set; } = 120;
        public string McpConfigPath { get; set; } = "./mcp-config.json";
        public bool AutoInstall { get; set; } = true;
        public string InstallCommand { get; set; } = "npm install -g @anthropic-ai/claude-code";
        public string ExtraArgs { get; set; } = "--no-confirm --json-output";
    }
}