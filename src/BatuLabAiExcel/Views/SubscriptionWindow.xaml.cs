using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using BatuLabAiExcel.ViewModels;

namespace BatuLabAiExcel.Views;

/// <summary>
/// Subscription management window
/// </summary>
public partial class SubscriptionWindow : Window
{
    public SubscriptionWindow()
    {
        InitializeComponent();
        var serviceProvider = ((App)Application.Current).ServiceProvider;
        var viewModel = serviceProvider.GetRequiredService<SubscriptionViewModel>();
        DataContext = viewModel;
        
        // Subscribe to property changes for UI updates
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.ErrorMessage))
            {
                ErrorMessageBorder.Visibility = string.IsNullOrEmpty(viewModel.ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (e.PropertyName == nameof(viewModel.StatusMessage))
            {
                StatusMessageBorder.Visibility = string.IsNullOrEmpty(viewModel.StatusMessage) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (e.PropertyName == nameof(viewModel.IsLoading))
            {
                LoadingPanel.Visibility = viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (e.PropertyName == nameof(viewModel.HasActiveLicense))
            {
                LicenseStatusCard.Visibility = viewModel.HasActiveLicense ? Visibility.Visible : Visibility.Collapsed;
            }
        };
        
        Loaded += async (s, e) =>
        {
            await viewModel.LoadCommand.ExecuteAsync(null);
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
        Close();
    }
}