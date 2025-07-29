using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using BatuLabAiExcel.Models;
using BatuLabAiExcel.Models.DTOs;
using BatuLabAiExcel.Services;
using BatuLabAiExcel.Views;

namespace BatuLabAiExcel.ViewModels;

/// <summary>
/// Main window view model with license management
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IChatOrchestrator _chatOrchestrator;
    private readonly IAuthenticationService _authService;
    private readonly ILicenseService _licenseService;
    private readonly IExcelProcessManager _excelProcessManager;
    private readonly ILogger<MainViewModel> _logger;
    private CancellationTokenSource? _currentOperationCts;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private string _currentFileName = "No file selected";

    [ObservableProperty]
    private string _selectedAiProvider = "Claude";

    [ObservableProperty]
    private string _currentAiProviderStatus = "Ready";

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private UserInfo? _currentUser;

    [ObservableProperty]
    private LicenseInfo? _currentLicense;

    [ObservableProperty]
    private string _licenseStatusText = string.Empty;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private bool _showProgress = false;

    [ObservableProperty]
    private bool _isLargeDataOperation = false;

    [ObservableProperty]
    private ObservableCollection<ProgressMessage> _progressMessages = new();

    [ObservableProperty]
    private bool _hasProgressMessages = false;

    public event Action? RequestScrollToBottom;

    public MainViewModel(
        IChatOrchestrator chatOrchestrator, 
        IAuthenticationService authService,
        ILicenseService licenseService,
        IExcelProcessManager excelProcessManager,
        ILogger<MainViewModel> logger)
    {
        _chatOrchestrator = chatOrchestrator;
        _authService = authService;
        _licenseService = licenseService;
        _excelProcessManager = excelProcessManager;
        _logger = logger;

        // Subscribe to authentication changes
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;

        // Initialize user info
        _ = Task.Run(async () => await InitializeUserInfoAsync());

        // Add welcome message
        Messages.Add(ChatMessage.CreateSystemMessage(
            "Welcome to Office Ai - Batu Lab.! 🎉\n\n" +
            "I can help you work with Excel files using AI. First, select an Excel file above, then try asking me to:\n" +
            "• Read data from Excel sheets\n" +
            "• Write data to specific cells or ranges\n" +
            "• Format cells and ranges\n" +
            "• Create charts and pivot tables\n" +
            "• Apply formulas and calculations\n\n" +
            "🛡️ DATA PROTECTION: I will NEVER modify, delete, or rearrange your existing data unless you specifically ask me to. " +
            "I only perform the exact actions you request and preserve all your existing work.\n\n" +
            "Example: \"Read data from Sheet1!A1:C10 and summarize it for me.\"\n\n" +
            "📊 PERFORMANCE: For testing large data operations, try: 'analyze all data' or 'process entire sheet'"));

        // Progress başlangıçta gizli - ihtiyaç olunca gösterilecek
        ShowProgress = false;
        ProgressText = string.Empty;
        IsLargeDataOperation = false;
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _currentOperationCts?.Cancel();
        _logger.LogInformation("User cancelled current operation");
    }
    
    private bool CanCancel => IsBusy;

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
                    "🛡️ PROTECTION MODE: Your existing data is safe! I will only perform the specific actions you request. " +
                    "I won't modify, delete, or rearrange your existing data unless you explicitly ask me to.\n\n" +
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

        SetBusy(true, "Initializing AI connection...");

        // Cancel any existing operation
        _currentOperationCts?.Cancel();
        _currentOperationCts = new CancellationTokenSource(TimeSpan.FromMinutes(10)); // 10 minute timeout

        try
        {
            _logger.LogInformation("Processing user message: {Message}", userMessage);
            
            // Clear previous progress messages and start new tracking
            ClearProgressMessages();
            AddProgressMessage($"Starting AI operation: {userMessage}", ProgressMessageType.AI);

            // Prepare Excel file if selected
            if (!string.IsNullOrEmpty(CurrentFilePath))
            {
                var prepareResult = await PrepareExcelFileAsync(CurrentFilePath, _currentOperationCts.Token);
                if (!prepareResult.IsSuccess)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var errorMessage = ChatMessage.CreateSystemMessage(
                            $"❌ Excel file preparation failed: {prepareResult.Error}");
                        Messages.Add(errorMessage);
                        RequestScrollToBottom?.Invoke();
                    });
                    return;
                }
            }

            // Include current file information in the context with Excel protection directives
            var contextualMessage = string.IsNullOrEmpty(CurrentFilePath) 
                ? $"No Excel file is currently selected. User message: {userMessage}"
                : $"Current Excel file: {CurrentFileName} (path: {CurrentFilePath}). CRITICAL EXCEL PROTECTION RULES - NEVER delete or clear existing data unless explicitly requested by the user - NEVER modify existing cell formatting unless explicitly requested - NEVER change existing formulas unless explicitly requested - NEVER rearrange or move existing data unless explicitly requested - When reading data use READ-ONLY operations - When adding new data use APPEND operations or write to empty cells only - Always preserve existing worksheets charts and pivot tables - Only perform the specific action requested by the user - If unclear about what to modify ASK the user for clarification first. User message: {userMessage}";

            // Update status and run processing on background thread
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SetBusy(true, "Processing with AI...");
            });

            // Detect if this might be a large data operation
            var isLargeDataOperation = DetectLargeDataOperation(userMessage);
            
            // Gerçek algılama mantığını kullan
            _logger.LogInformation("Large data operation detected: {IsLarge} for message: {Message}", isLargeDataOperation, userMessage);
            
            // Run the processing on a background thread to avoid UI blocking
            Result<string> result;
            
            if (isLargeDataOperation)
            {
                // Use progress reporting for large operations
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLargeDataOperation = true;
                    ShowProgress = true;
                    ProgressText = "Preparing for large data operation...";
                });

                var progress = new Progress<string>(progressMessage =>
                {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ProgressText = progressMessage;
                        // Also add to detailed progress tab
                        AddProgressMessage(progressMessage, ProgressMessageType.Processing);
                    });
                });

                result = await Task.Run(async () => 
                {
                    try
                    {
                        return await _chatOrchestrator.ProcessMessageWithProgressAsync(
                            contextualMessage, 
                            progress, 
                            _currentOperationCts.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in background processing with progress");
                        return Result<string>.Failure($"Background processing error: {ex.Message}");
                    }
                }, _currentOperationCts.Token);
            }
            else
            {
                // Use standard processing for normal operations
                result = await Task.Run(async () => 
                {
                    try
                    {
                        return await _chatOrchestrator.ProcessMessageAsync(contextualMessage, _currentOperationCts.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in background processing");
                        return Result<string>.Failure($"Background processing error: {ex.Message}");
                    }
                }, _currentOperationCts.Token);
            }

            if (_currentOperationCts.Token.IsCancellationRequested)
            {
                return;
            }

            // Update UI on the UI thread
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Hide progress UI
                ShowProgress = false;
                IsLargeDataOperation = false;
                ProgressText = string.Empty;

                if (result.IsSuccess)
                {
                    var assistantMessage = ChatMessage.CreateAssistantMessage(result.Value ?? "No response received.");
                    Messages.Add(assistantMessage);
                    _logger.LogInformation("Successfully processed message");
                    AddProgressMessage("AI operation completed successfully", ProgressMessageType.Success);
                }
                else
                {
                    var errorMessage = ChatMessage.CreateSystemMessage(
                        $"❌ Error: {result.Error}\n\nPlease check your configuration and try again.");
                    Messages.Add(errorMessage);
                    _logger.LogError("Error processing message: {Error}", result.Error);
                    AddProgressMessage($"AI operation failed: {result.Error}", ProgressMessageType.Error);
                }
                RequestScrollToBottom?.Invoke();
            });

            // Open Excel file after processing (if there was a file selected and operation was successful)
            if (!string.IsNullOrEmpty(CurrentFilePath) && result.IsSuccess)
            {
                // Wait a bit for any file operations to complete
                await Task.Delay(1000, _currentOperationCts.Token);
                await OpenExcelFileAfterProcessingAsync(CurrentFilePath);
            }
        }
        catch (OperationCanceledException) when (_currentOperationCts?.Token.IsCancellationRequested == true)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Hide progress UI
                ShowProgress = false;
                IsLargeDataOperation = false;
                ProgressText = string.Empty;
                
                var errorMessage = ChatMessage.CreateSystemMessage(
                    "⏸️ Operation was cancelled or timed out.\n\nTry breaking your request into smaller parts or check your connection.");
                Messages.Add(errorMessage);
                RequestScrollToBottom?.Invoke();
            });
            _logger.LogWarning("Operation was cancelled or timed out");
        }
        catch (Exception ex)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var errorMessage = ChatMessage.CreateSystemMessage(
                    $"❌ Unexpected error: {ex.Message}\n\nPlease try again or contact support if the problem persists.");
                Messages.Add(errorMessage);
                RequestScrollToBottom?.Invoke();
            });
            _logger.LogError(ex, "Unexpected error processing message");
        }
        finally
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SetBusy(false);
            });
            _currentOperationCts?.Dispose();
            _currentOperationCts = null;
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
            CancelCommand.NotifyCanExecuteChanged();
        }
    }

    public void ChangeAiProvider(string provider)
    {
        try
        {
            if (string.IsNullOrEmpty(provider))
                return;

            var previousProvider = SelectedAiProvider;
            SelectedAiProvider = provider;
            CurrentAiProviderStatus = $"Switched to {provider}";

            // Update the orchestrator's current provider
            _chatOrchestrator.SetCurrentProvider(provider);

            _logger.LogInformation("AI Provider changed from {Previous} to {Current}", previousProvider, provider);

            // Add system message to inform user
            var message = ChatMessage.CreateSystemMessage($"🔄 AI Provider switched to {provider}");
            Messages.Add(message);
            RequestScrollToBottom?.Invoke();

            // Reset status after 3 seconds
            _ = Task.Delay(3000).ContinueWith(_ =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentAiProviderStatus = "Ready";
                });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing AI provider to {Provider}", provider);
            CurrentAiProviderStatus = "Error switching provider";
        }
    }

    [RelayCommand]
    private void ShowSubscriptionManager()
    {
        try
        {
            var subscriptionWindow = new SubscriptionWindow();
            subscriptionWindow.Show();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening subscription manager");
            Messages.Add(ChatMessage.CreateSystemMessage($"❌ Error opening subscription manager: {ex.Message}"));
            RequestScrollToBottom?.Invoke();
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            await _authService.LogoutAsync();
            _logger.LogInformation("User logged out successfully");
            
            // Close main window and show login
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            
            Application.Current.MainWindow?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            Messages.Add(ChatMessage.CreateSystemMessage($"❌ Error during logout: {ex.Message}"));
            RequestScrollToBottom?.Invoke();
        }
    }

    private async Task InitializeUserInfoAsync()
    {
        try
        {
            CurrentUser = await _authService.GetCurrentUserAsync();
            
            if (CurrentUser != null)
            {
                var validationResult = await _licenseService.ValidateLicenseAsync(CurrentUser.Id);
                if (validationResult.IsValid && validationResult.License != null)
                {
                    CurrentLicense = validationResult.License;
                    UpdateLicenseStatusText();
                }
                else
                {
                    LicenseStatusText = "No active license";
                    
                    // If license is expired or invalid, show subscription window
                    if (CurrentLicense != null && CurrentLicense.IsExpired)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                var serviceProvider = ((App)Application.Current).ServiceProvider;
                                var subscriptionWindow = serviceProvider.GetRequiredService<SubscriptionWindow>();
                                subscriptionWindow.ShowDialog();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error opening subscription window");
                            }
                        }));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing user info");
        }
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        _ = Task.Run(async () =>
        {
            if (isAuthenticated)
            {
                await InitializeUserInfoAsync();
            }
            else
            {
                CurrentUser = null;
                CurrentLicense = null;
                LicenseStatusText = string.Empty;
            }
        });
    }

    private void UpdateLicenseStatusText()
    {
        if (CurrentLicense == null)
        {
            LicenseStatusText = "No license";
            return;
        }

        var statusText = CurrentLicense.TypeDisplayName;
        
        if (CurrentLicense.Type != Models.Entities.LicenseType.Lifetime)
        {
            var days = CurrentLicense.DaysRemaining;
            var timeText = days > 1 ? $"{days} days" : 
                          days == 1 ? "1 day" : "Expired";
            statusText += $" - {timeText} remaining";
        }
        
        LicenseStatusText = statusText;
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        try
        {
            var settingsWindow = App.StaticServiceProvider.GetRequiredService<SettingsWindow>();
            settingsWindow.Owner = System.Windows.Application.Current.MainWindow;
            
            var result = settingsWindow.ShowDialog();
            if (result == true)
            {
                _logger.LogInformation("Settings saved successfully");
                // Optionally refresh any settings-dependent components
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening settings window");
            Messages.Add(ChatMessage.CreateSystemMessage($"❌ Error opening settings: {ex.Message}"));
        }
    }

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        try
        {
            var aboutMessage = @"🏢 Office Ai - Batu Lab.
Version 1.0.0

AI-powered Excel automation with Claude integration.
This application helps you work with Excel files using natural language.

🛡️ Privacy & Security:
• Your data never leaves your computer
• API keys are stored securely in Windows Credential Manager
• All Excel operations are performed locally

📧 Support: support@batulab.com
🌐 Website: https://batulab.com

© 2025 Batu Lab. All rights reserved.";

            Messages.Add(ChatMessage.CreateSystemMessage(aboutMessage));
            RequestScrollToBottom?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing about dialog");
        }
    }

    /// <summary>
    /// Detect if a message might involve large data operations that would benefit from progress reporting
    /// </summary>
    private bool DetectLargeDataOperation(string message)
    {
        var largeDataKeywords = new[]
        {
            "all data", "entire sheet", "whole workbook", "large dataset", "thousands", "hundreds",
            "bulk", "batch", "import", "export", "copy all", "process all", "analyze all",
            "pivot table", "chart", "massive", "big data", "full range", "complete data",
            // Daha fazla keyword ekleyelim
            "tüm veri", "tüm sheet", "bütün", "toplu", "hepsi", "analiz et", "graf", "grafik"
        };

        var lowerMessage = message.ToLowerInvariant();
        var isLarge = largeDataKeywords.Any(keyword => lowerMessage.Contains(keyword));
        
        // Debug için log ekleyelim
        _logger.LogInformation("DetectLargeDataOperation: Message='{Message}' -> IsLarge={IsLarge}", message, isLarge);
        
        return isLarge;
    }

    /// <summary>
    /// Add progress message to the progress tab
    /// </summary>
    private void AddProgressMessage(string message, ProgressMessageType type = ProgressMessageType.Info)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var progressMessage = ProgressMessage.Create(message, type);
            ProgressMessages.Add(progressMessage);
            HasProgressMessages = ProgressMessages.Any();
            
            // Keep only last 100 messages to prevent memory issues
            if (ProgressMessages.Count > 100)
            {
                ProgressMessages.RemoveAt(0);
            }
        });
    }

    /// <summary>
    /// Clear progress messages
    /// </summary>
    private void ClearProgressMessages()
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ProgressMessages.Clear();
            HasProgressMessages = false;
        });
    }

    /// <summary>
    /// Handle Excel file before processing
    /// </summary>
    private async Task<Result> PrepareExcelFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            AddProgressMessage("Checking Excel file status...", ProgressMessageType.Excel);
            
            var isOpenResult = await _excelProcessManager.IsExcelFileOpenAsync(filePath);
            if (!isOpenResult.IsSuccess)
            {
                AddProgressMessage($"Warning: Could not check Excel file status: {isOpenResult.Error}", ProgressMessageType.Warning);
                return Result.Success(); // Continue anyway
            }

            if (isOpenResult.Value)
            {
                AddProgressMessage("Excel file is currently open, closing it for processing...", ProgressMessageType.Excel);
                
                var closeResult = await _excelProcessManager.CloseExcelFileAsync(filePath);
                if (!closeResult.IsSuccess)
                {
                    AddProgressMessage($"Warning: Could not close Excel file: {closeResult.Error}", ProgressMessageType.Warning);
                    // Continue anyway, might still work
                }
                else
                {
                    AddProgressMessage("Excel file closed successfully", ProgressMessageType.Success);
                    await Task.Delay(1000, cancellationToken); // Wait for file to be unlocked
                }
            }
            else
            {
                AddProgressMessage("Excel file is ready for processing", ProgressMessageType.Success);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing Excel file: {FilePath}", filePath);
            AddProgressMessage($"Error preparing Excel file: {ex.Message}", ProgressMessageType.Error);
            return Result.Failure($"Error preparing Excel file: {ex.Message}");
        }
    }

    /// <summary>
    /// Open Excel file after processing
    /// </summary>
    private async Task OpenExcelFileAfterProcessingAsync(string filePath)
    {
        try
        {
            AddProgressMessage("Opening Excel file to show changes...", ProgressMessageType.Excel);
            
            var openResult = await _excelProcessManager.OpenExcelFileAsync(filePath);
            if (openResult.IsSuccess)
            {
                AddProgressMessage("Excel file opened successfully", ProgressMessageType.Success);
            }
            else
            {
                AddProgressMessage($"Could not open Excel file: {openResult.Error}", ProgressMessageType.Warning);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening Excel file after processing: {FilePath}", filePath);
            AddProgressMessage($"Error opening Excel file: {ex.Message}", ProgressMessageType.Error);
        }
    }
}