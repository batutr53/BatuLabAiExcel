using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using BatuLabAiExcel.Models.DTOs;
using BatuLabAiExcel.Models.Entities;
using BatuLabAiExcel.Services;

namespace BatuLabAiExcel.ViewModels;

/// <summary>
/// ViewModel for subscription management
/// </summary>
public partial class SubscriptionViewModel : ObservableObject
{
    private readonly IPaymentService _paymentService;
    private readonly IAuthenticationService _authService;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<SubscriptionViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<SubscriptionPlan> availablePlans = new();

    [ObservableProperty]
    private LicenseInfo? currentLicense;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasActiveLicense = false;

    [ObservableProperty]
    private string licenseStatusText = string.Empty;

    [ObservableProperty]
    private string remainingTimeText = string.Empty;

    public SubscriptionViewModel(
        IPaymentService paymentService,
        IAuthenticationService authService,
        ILicenseService licenseService,
        ILogger<SubscriptionViewModel> logger)
    {
        _paymentService = paymentService;
        _authService = authService;
        _licenseService = licenseService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            // Load available plans
            var plans = await _paymentService.GetSubscriptionPlansAsync();
            AvailablePlans.Clear();
            foreach (var plan in plans)
            {
                AvailablePlans.Add(plan);
            }

            // Load current license info
            await RefreshLicenseInfoAsync();

            _logger.LogInformation("Subscription page loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading subscription information");
            ErrorMessage = "Failed to load subscription information. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PurchaseAsync(SubscriptionPlan plan)
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = "Creating checkout session...";

            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                ErrorMessage = "Please log in to purchase a subscription.";
                return;
            }

            var request = new CreatePaymentRequest
            {
                LicenseType = plan.Type,
                SuccessUrl = "https://your-app.com/payment-success",
                CancelUrl = "https://your-app.com/payment-cancelled"
            };

            var result = await _paymentService.CreateCheckoutSessionAsync(request, currentUser.Id);

            if (result.Success && !string.IsNullOrEmpty(result.CheckoutUrl))
            {
                StatusMessage = "Redirecting to payment...";
                
                // Open Stripe checkout in default browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = result.CheckoutUrl,
                    UseShellExecute = true
                });

                StatusMessage = "Payment window opened in browser. Complete the payment and return to the app.";
                _logger.LogInformation("Checkout session created for user: {UserId}, Plan: {PlanType}", currentUser.Id, plan.Type);
            }
            else
            {
                ErrorMessage = result.Message;
                if (result.Errors.Any())
                {
                    ErrorMessage += "\n" + string.Join("\n", result.Errors);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for plan: {PlanType}", plan.Type);
            ErrorMessage = "Failed to create payment session. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshLicenseAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Refreshing license information...";

            await RefreshLicenseInfoAsync();

            StatusMessage = "License information updated.";
            _logger.LogInformation("License information refreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing license information");
            ErrorMessage = "Failed to refresh license information.";
        }
        finally
        {
            IsLoading = false;
            StatusMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ManageBillingAsync()
    {
        if (CurrentLicense == null) return;

        try
        {
            StatusMessage = "Opening billing portal...";

            // This would require storing the Stripe customer ID
            // For now, we'll show a message
            StatusMessage = "Billing management will be available soon. Please contact support for billing changes.";

            await Task.Delay(3000);
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening billing portal");
            ErrorMessage = "Failed to open billing portal.";
        }
    }

    private async Task RefreshLicenseInfoAsync()
    {
        var currentUser = await _authService.GetCurrentUserAsync();
        if (currentUser == null) return;

        var validationResult = await _licenseService.ValidateLicenseAsync(currentUser.Id);
        
        if (validationResult.IsValid && validationResult.License != null)
        {
            CurrentLicense = validationResult.License;
            HasActiveLicense = true;
            LicenseStatusText = $"Active {CurrentLicense.TypeDisplayName}";
            
            if (CurrentLicense.Type == LicenseType.Lifetime)
            {
                RemainingTimeText = "Lifetime access";
            }
            else
            {
                var days = CurrentLicense.DaysRemaining;
                RemainingTimeText = days > 1 ? $"{days} days remaining" : 
                                   days == 1 ? "Expires today" : "Expired";
            }
        }
        else
        {
            CurrentLicense = null;
            HasActiveLicense = false;
            LicenseStatusText = "No active license";
            RemainingTimeText = "Trial expired or no license";
        }
    }

    public void ClearMessages()
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
    }
}