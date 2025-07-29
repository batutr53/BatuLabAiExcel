namespace BatuLabAiExcel.Models;

/// <summary>
/// Represents a progress message for real-time operation tracking
/// </summary>
public class ProgressMessage
{
    public string Icon { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public ProgressMessageType Type { get; set; } = ProgressMessageType.Info;

    public static ProgressMessage Create(string message, ProgressMessageType type = ProgressMessageType.Info)
    {
        return new ProgressMessage
        {
            Message = message,
            Type = type,
            Icon = GetIconForType(type),
            Timestamp = DateTime.Now
        };
    }

    private static string GetIconForType(ProgressMessageType type)
    {
        return type switch
        {
            ProgressMessageType.Info => "ℹ️",
            ProgressMessageType.Success => "✅",
            ProgressMessageType.Warning => "⚠️",
            ProgressMessageType.Error => "❌",
            ProgressMessageType.Processing => "⚙️",
            ProgressMessageType.Python => "🐍",
            ProgressMessageType.Excel => "📊",
            ProgressMessageType.AI => "🤖",
            ProgressMessageType.MCP => "🔗",
            _ => "ℹ️"
        };
    }
}

public enum ProgressMessageType
{
    Info,
    Success,
    Warning,
    Error,
    Processing,
    Python,
    Excel,
    AI,
    MCP
}