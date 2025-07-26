using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using BatuLabAiExcel.ViewModels;

namespace BatuLabAiExcel.Views;

/// <summary>
/// Registration window for new user accounts
/// </summary>
public partial class RegisterWindow : Window
{
    public RegisterWindow()
    {
        InitializeComponent();
        var serviceProvider = ((App)Application.Current).ServiceProvider;
        var viewModel = serviceProvider.GetRequiredService<RegisterViewModel>();
        DataContext = viewModel;
        
        // Subscribe to property changes for UI updates
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.ErrorMessage))
            {
                ErrorBorder.Visibility = string.IsNullOrEmpty(viewModel.ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (e.PropertyName == nameof(viewModel.SuccessMessage))
            {
                SuccessBorder.Visibility = string.IsNullOrEmpty(viewModel.SuccessMessage) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (e.PropertyName == nameof(viewModel.IsLoading))
            {
                LoadingPanel.Visibility = viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
            }
        };
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
        if (DataContext is RegisterViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.ConfirmPassword = passwordBox.Password;
        }
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel viewModel)
        {
            if (viewModel.CanRegister)
            {
                await viewModel.RegisterCommand.ExecuteAsync(null);
                
                // Check if registration was successful
                if (viewModel.IsRegistrationSuccessful)
                {
                    // Open main window
                    var serviceProvider = ((App)Application.Current).ServiceProvider;
                    var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
                    Application.Current.MainWindow = mainWindow;
                    mainWindow.Show();
                    
                    // Close register window
                    Close();
                }
            }
            else
            {
                MessageBox.Show("Please complete all required fields and accept the terms.", 
                               "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

}