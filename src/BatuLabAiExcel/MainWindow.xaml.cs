using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using BatuLabAiExcel.ViewModels;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel;

/// <summary>
/// Main window of the application
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Set DataContext from DI
        var serviceProvider = ((App)Application.Current).ServiceProvider;
        var viewModel = serviceProvider.GetRequiredService<MainViewModel>();
        DataContext = viewModel;
        
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel viewModel)
        {
            viewModel.RequestScrollToBottom += () => Dispatcher.Invoke(ScrollToBottom);
            InputTextBox.Focus();
        }
    }

    private void ScrollToBottom()
    {
        ChatScrollViewer.ScrollToBottom();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        InputTextBox.Focus();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        // Check if the dragged data contains files
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var excelFiles = files?.Where(f => f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || 
                                              f.EndsWith(".xls", StringComparison.OrdinalIgnoreCase));
            
            if (excelFiles?.Any() == true)
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var excelFile = files?.FirstOrDefault(f => f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || 
                                                          f.EndsWith(".xls", StringComparison.OrdinalIgnoreCase));
                
                if (!string.IsNullOrEmpty(excelFile) && DataContext is MainViewModel viewModel)
                {
                    viewModel.SetCurrentFile(excelFile);
                    
                    viewModel.Messages.Add(Models.ChatMessage.CreateSystemMessage(
                        $"📁 Excel file dropped: {Path.GetFileName(excelFile)}\n" +
                        $"🛡️ PROTECTION MODE: Your existing data is safe! I will only perform the specific actions you request. " +
                        $"File has been loaded and is ready for AI processing!"));
                    
                    ScrollToBottom();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading dropped file: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        e.Handled = true;
    }

    private void MessageBorder_RightClick(object sender, MouseButtonEventArgs e)
    {
        // Context menu will automatically show
    }

    private void CopyMessage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is ChatMessage message)
        {
            try
            {
                var fullMessage = $"{message.Role}: {message.Content}";
                Clipboard.SetText(fullMessage);
                ShowCopyNotification("Message copied to clipboard!");
            }
            catch (Exception ex)
            {
                ShowCopyNotification($"Copy failed: {ex.Message}");
            }
        }
    }

    private void CopyContent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is ChatMessage message)
        {
            try
            {
                Clipboard.SetText(message.Content);
                ShowCopyNotification("Content copied to clipboard!");
            }
            catch (Exception ex)
            {
                ShowCopyNotification($"Copy failed: {ex.Message}");
            }
        }
    }

    private void CopyWithTimestamp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is ChatMessage message)
        {
            try
            {
                var timestampedMessage = $"[{message.Timestamp:HH:mm:ss}] {message.Role}: {message.Content}";
                Clipboard.SetText(timestampedMessage);
                ShowCopyNotification("Message with timestamp copied!");
            }
            catch (Exception ex)
            {
                ShowCopyNotification($"Copy failed: {ex.Message}");
            }
        }
    }

    private void ShowCopyNotification(string message)
    {
        if (DataContext is MainViewModel viewModel)
        {
            // Add a temporary system message to show copy success
            var notification = ChatMessage.CreateSystemMessage($"📋 {message}");
            viewModel.Messages.Add(notification);
            
            // Remove the notification after 3 seconds
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (viewModel.Messages.Contains(notification))
                {
                    viewModel.Messages.Remove(notification);
                }
            };
            
            timer.Start();
            ScrollToBottom();
        }
    }

    private void AiProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var provider = selectedItem.Tag?.ToString();
            if (DataContext is MainViewModel viewModel && !string.IsNullOrEmpty(provider))
            {
                viewModel.ChangeAiProvider(provider);
            }
        }
    }

    // Window control methods for custom title bar
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

/// <summary>
/// Converter for message alignment based on user role
/// </summary>
public class MessageAlignmentConverter : IValueConverter
{
    public static readonly MessageAlignmentConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isUser)
        {
            return isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }
        return HorizontalAlignment.Left;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}