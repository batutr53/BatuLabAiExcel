using CommunityToolkit.Mvvm.ComponentModel;

namespace BatuLabAiExcel.Models;

/// <summary>
/// Represents a chat message in the UI
/// </summary>
public partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    private string _role;

    [ObservableProperty]
    private string _content;

    [ObservableProperty]
    private DateTime _timestamp;

    [ObservableProperty]
    private bool _isUser;

    public ChatMessage(string role, string content, bool isUser = false)
    {
        _role = role;
        _content = content;
        _isUser = isUser;
        _timestamp = DateTime.Now;
    }

    public static ChatMessage CreateUserMessage(string content) =>
        new("You", content, isUser: true);

    public static ChatMessage CreateAssistantMessage(string content) =>
        new("Assistant", content, isUser: false);

    public static ChatMessage CreateSystemMessage(string content) =>
        new("System", content, isUser: false);
}