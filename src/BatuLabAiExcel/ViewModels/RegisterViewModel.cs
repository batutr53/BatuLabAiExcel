using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using BatuLabAiExcel.Models.DTOs;
using BatuLabAiExcel.Services;

namespace BatuLabAiExcel.ViewModels;

/// <summary>
/// ViewModel for user registration functionality
/// </summary>
public partial class RegisterViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<RegisterViewModel> _logger;

    [ObservableProperty]
    private string firstName = string.Empty;

    [ObservableProperty]
    private string lastName = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private bool acceptTerms = false;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isRegistrationSuccessful = false;

    [ObservableProperty]
    private string successMessage = string.Empty;

    public RegisterViewModel(IAuthenticationService authService, ILogger<RegisterViewModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        if (IsLoading) return;

        try
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            IsLoading = true;

            // Validate passwords match
            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            var request = new RegisterRequest
            {
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Email = Email.Trim(),
                Password = Password,
                ConfirmPassword = ConfirmPassword,
                AcceptTerms = AcceptTerms
            };

            var result = await _authService.RegisterAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("User registered successfully: {Email}", Email);
                IsRegistrationSuccessful = true;
                SuccessMessage = result.Message;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = result.Message;
                if (result.Errors.Any())
                {
                    ErrorMessage += "\n" + string.Join("\n", result.Errors);
                }
                _logger.LogWarning("Registration failed for user: {Email}, Message: {Message}", Email, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration attempt for user: {Email}", Email);
            ErrorMessage = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private void Reset()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        AcceptTerms = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsRegistrationSuccessful = false;
        IsLoading = false;
    }

    public bool CanRegister => !IsLoading && 
                              !string.IsNullOrWhiteSpace(FirstName) &&
                              !string.IsNullOrWhiteSpace(LastName) &&
                              !string.IsNullOrWhiteSpace(Email) &&
                              !string.IsNullOrWhiteSpace(Password) &&
                              !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                              AcceptTerms;

    partial void OnFirstNameChanged(string value)
    {
        ClearMessages();
        RegisterCommand.NotifyCanExecuteChanged();
    }

    partial void OnLastNameChanged(string value)
    {
        ClearMessages();
        RegisterCommand.NotifyCanExecuteChanged();
    }

    partial void OnEmailChanged(string value)
    {
        ClearMessages();
        RegisterCommand.NotifyCanExecuteChanged();
    }

    partial void OnPasswordChanged(string value)
    {
        ClearMessages();
        RegisterCommand.NotifyCanExecuteChanged();
    }

    partial void OnConfirmPasswordChanged(string value)
    {
        ClearMessages();
        RegisterCommand.NotifyCanExecuteChanged();
    }

    partial void OnAcceptTermsChanged(bool value)
    {
        ClearMessages();
        RegisterCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        RegisterCommand.NotifyCanExecuteChanged();
    }
}