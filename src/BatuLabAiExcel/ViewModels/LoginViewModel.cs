using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using BatuLabAiExcel.Models.DTOs;
using BatuLabAiExcel.Services;

namespace BatuLabAiExcel.ViewModels;

/// <summary>
/// ViewModel for login functionality
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool rememberMe = false;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isLoginSuccessful = false;

    public LoginViewModel(IAuthenticationService authService, ILogger<LoginViewModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        if (IsLoading) return;

        try
        {
            ErrorMessage = string.Empty;
            IsLoading = true;

            var request = new LoginRequest
            {
                Email = Email.Trim(),
                Password = Password,
                RememberMe = RememberMe
            };

            var result = await _authService.LoginAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("User logged in successfully: {Email}", Email);
                IsLoginSuccessful = true;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = result.Message;
                _logger.LogWarning("Login failed for user: {Email}, Message: {Message}", Email, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt for user: {Email}", Email);
            ErrorMessage = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearError()
    {
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void Reset()
    {
        Email = string.Empty;
        Password = string.Empty;
        RememberMe = false;
        ErrorMessage = string.Empty;
        IsLoginSuccessful = false;
        IsLoading = false;
    }

    public bool CanLogin => !IsLoading && 
                           !string.IsNullOrWhiteSpace(Email) && 
                           !string.IsNullOrWhiteSpace(Password);

    partial void OnEmailChanged(string value)
    {
        ErrorMessage = string.Empty;
        LoginCommand.NotifyCanExecuteChanged();
    }

    partial void OnPasswordChanged(string value)
    {
        ErrorMessage = string.Empty;
        LoginCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        LoginCommand.NotifyCanExecuteChanged();
    }
}