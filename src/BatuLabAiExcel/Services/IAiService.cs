using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Unified interface for AI services (Claude, Gemini, etc.)
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Current AI provider name
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Send a message with optional tools and get response
    /// </summary>
    Task<Result<AiResponse>> SendMessageAsync(
        List<AiMessage> messages,
        List<AiTool>? tools = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Unified AI message format
/// </summary>
public class AiMessage
{
    public string Role { get; set; } = string.Empty;
    public List<AiMessageContent> Content { get; set; } = new();
}

/// <summary>
/// Unified AI message content
/// </summary>
public class AiMessageContent
{
    public string Type { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string? ToolUseId { get; set; }
    public string? ToolName { get; set; }
    public object? ToolInput { get; set; }
    public string? ToolResult { get; set; }
    public bool? IsError { get; set; }
}

/// <summary>
/// Unified AI tool definition
/// </summary>
public class AiTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AiToolSchema Parameters { get; set; } = new();
}

/// <summary>
/// Unified AI tool schema
/// </summary>
public class AiToolSchema
{
    public string Type { get; set; } = "object";
    public Dictionary<string, AiToolProperty>? Properties { get; set; }
    public List<string>? Required { get; set; }
}

/// <summary>
/// Unified AI tool property
/// </summary>
public class AiToolProperty
{
    public string Type { get; set; } = "string";
    public string? Description { get; set; }
    public List<string>? Enum { get; set; }
    public AiToolProperty? Items { get; set; }
}

/// <summary>
/// Unified AI response
/// </summary>
public class AiResponse
{
    public string Id { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public List<AiResponseContent> Content { get; set; } = new();
    public AiUsage? Usage { get; set; }
    public string? FinishReason { get; set; }
}

/// <summary>
/// Unified AI response content
/// </summary>
public class AiResponseContent
{
    public string Type { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string? ToolUseId { get; set; }
    public string? ToolName { get; set; }
    public object? ToolInput { get; set; }
}

/// <summary>
/// Unified AI usage information
/// </summary>
public class AiUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
}