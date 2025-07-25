using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using BatuLabAiExcel.Models;
using BatuLabAiExcel.Services;

namespace BatuLabAiExcel.ViewModels;

/// <summary>
/// Main window view model
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IChatOrchestrator _chatOrchestrator;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private string _currentFileName = "No file selected";

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    public event Action? RequestScrollToBottom;

    public MainViewModel(IChatOrchestrator chatOrchestrator, ILogger<MainViewModel> logger)
    {
        _chatOrchestrator = chatOrchestrator;
        _logger = logger;

        // Add welcome message
        Messages.Add(ChatMessage.CreateSystemMessage(
            "Welcome to Office Ai - Batu Lab.! 🎉\n\n" +
            "I can help you work with Excel files using AI. First, select an Excel file above, then try asking me to:\n" +
            "• Read data from Excel sheets\n" +
            "• Write data to specific cells or ranges\n" +
            "• Format cells and ranges\n" +
            "• Create charts and pivot tables\n" +
            "• Apply formulas and calculations\n\n" +
            "Example: \"Read data from Sheet1!A1:C10 and summarize it for me.\""));
    }

    [RelayCommand]
    private void BrowseFile()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Excel File",
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;
                SetCurrentFile(filePath);
                
                Messages.Add(ChatMessage.CreateSystemMessage(
                    $"✅ Excel file loaded: {Path.GetFileName(filePath)}\n" +
                    $"📁 Path: {filePath}\n\n" +
                    "You can now ask me to work with this file!"));
                
                RequestScrollToBottom?.Invoke();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing file");
            Messages.Add(ChatMessage.CreateSystemMessage(
                $"❌ Error selecting file: {ex.Message}"));
            RequestScrollToBottom?.Invoke();
        }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        try
        {
            var workingDirectory = Path.GetFullPath("./excel_files");
            
            if (!Directory.Exists(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
                _logger.LogInformation("Created working directory: {Directory}", workingDirectory);
            }

            // Open folder in Windows Explorer
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = workingDirectory,
                UseShellExecute = true
            });

            Messages.Add(ChatMessage.CreateSystemMessage(
                $"📂 Opened working folder: {workingDirectory}\n\n" +
                "You can copy your Excel files here, then use the Browse button to select them."));
            
            RequestScrollToBottom?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening folder");
            Messages.Add(ChatMessage.CreateSystemMessage(
                $"❌ Error opening folder: {ex.Message}"));
            RequestScrollToBottom?.Invoke();
        }
    }

    public void SetCurrentFile(string filePath)
    {
        try
        {
            // Copy file to working directory if it's not already there
            var workingDirectory = Path.GetFullPath("./excel_files");
            if (!Directory.Exists(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
            }

            var fileName = Path.GetFileName(filePath);
            var targetPath = Path.Combine(workingDirectory, fileName);

            // Copy file if source is different from target
            if (!string.Equals(Path.GetFullPath(filePath), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(filePath, targetPath, overwrite: true);
                _logger.LogInformation("Copied file from {Source} to {Target}", filePath, targetPath);
                
                Messages.Add(ChatMessage.CreateSystemMessage(
                    $"📋 File copied to working directory for safe processing."));
            }

            CurrentFilePath = targetPath;
            CurrentFileName = fileName;
            
            _logger.LogInformation("Set current file: {FileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting current file");
            throw;
        }
    }

    [RelayCommand]
    private void CopyAllMessages()
    {
        try
        {
            var allMessages = string.Join("\n\n", Messages.Select(m => 
                $"[{m.Timestamp:HH:mm:ss}] {m.Role}: {m.Content}"));
            
            if (!string.IsNullOrEmpty(allMessages))
            {
                System.Windows.Clipboard.SetText(allMessages);
                Messages.Add(ChatMessage.CreateSystemMessage("📋 All messages copied to clipboard!"));
                RequestScrollToBottom?.Invoke();
                
                // Remove notification after 3 seconds
                Task.Delay(3000).ContinueWith(_ =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var notification = Messages.LastOrDefault(m => m.Content.Contains("All messages copied"));
                        if (notification != null)
                        {
                            Messages.Remove(notification);
                        }
                    });
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying all messages");
            Messages.Add(ChatMessage.CreateSystemMessage($"❌ Copy failed: {ex.Message}"));
            RequestScrollToBottom?.Invoke();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
            return;

        var userMessage = InputText.Trim();
        InputText = string.Empty;

        // Add user message
        var chatMessage = ChatMessage.CreateUserMessage(userMessage);
        Messages.Add(chatMessage);
        RequestScrollToBottom?.Invoke();

        SetBusy(true, "Processing your request...");

        try
        {
            _logger.LogInformation("Processing user message: {Message}", userMessage);

            // Include current file information in the context
            var contextualMessage = string.IsNullOrEmpty(CurrentFilePath) 
                ? $"No Excel file is currently selected. User message: {userMessage}"
                : $"Current Excel file: {CurrentFileName} (path: {CurrentFilePath}). User message: {userMessage}";

            var result = await _chatOrchestrator.ProcessMessageAsync(contextualMessage, CancellationToken.None);

            if (result.IsSuccess)
            {
                var assistantMessage = ChatMessage.CreateAssistantMessage(result.Value ?? "No response received.");
                Messages.Add(assistantMessage);
                _logger.LogInformation("Successfully processed message");
            }
            else
            {
                var errorMessage = ChatMessage.CreateSystemMessage(
                    $"❌ Error: {result.Error}\n\nPlease check your configuration and try again.");
                Messages.Add(errorMessage);
                _logger.LogError("Error processing message: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = ChatMessage.CreateSystemMessage(
                $"❌ Unexpected error: {ex.Message}\n\nPlease try again or contact support if the problem persists.");
            Messages.Add(errorMessage);
            _logger.LogError(ex, "Unexpected error processing message");
        }
        finally
        {
            SetBusy(false);
            RequestScrollToBottom?.Invoke();
        }
    }

    private bool CanSend() => !IsBusy && !string.IsNullOrWhiteSpace(InputText);

    partial void OnInputTextChanged(string value)
    {
        SendCommand.NotifyCanExecuteChanged();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        
        if (e.PropertyName == nameof(IsBusy))
        {
            SendCommand.NotifyCanExecuteChanged();
        }
    }
}