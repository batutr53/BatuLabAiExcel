using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using BatuLabAiExcel.ViewModels;

namespace BatuLabAiExcel.Views;

/// <summary>
/// Login window for user authentication
/// </summary>
public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        var serviceProvider = ((App)Application.Current).ServiceProvider;
        var viewModel = serviceProvider.GetRequiredService<LoginViewModel>();
        DataContext = viewModel;
        
        // Subscribe to property changes for UI updates
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.ErrorMessage))
            {
                ErrorBorder.Visibility = string.IsNullOrEmpty(viewModel.ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (e.PropertyName == nameof(viewModel.IsLoading))
            {
                LoadingPanel.Visibility = viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
            }
        };
        
        // Focus on email textbox when window loads
        Loaded += (s, e) => EmailTextBox.Focus();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        var registerWindow = new RegisterWindow();
        registerWindow.Show();
        Close();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            if (viewModel.CanLogin)
            {
                await viewModel.LoginCommand.ExecuteAsync(null);
                
                // Check if login was successful
                if (viewModel.IsLoginSuccessful)
                {
                    // Open main window
                    var serviceProvider = ((App)Application.Current).ServiceProvider;
                    var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
                    Application.Current.MainWindow = mainWindow;
                    mainWindow.Show();
                    
                    // Close login window
                    Close();
                }
            }
            else
            {
                MessageBox.Show("Please fill in both email and password fields.", "Validation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

}

