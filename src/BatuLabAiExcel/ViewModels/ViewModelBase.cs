using CommunityToolkit.Mvvm.ComponentModel;

namespace BatuLabAiExcel.ViewModels;

/// <summary>
/// Base class for all view models
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Set busy state with optional status message
    /// </summary>
    protected void SetBusy(bool busy, string? message = null)
    {
        IsBusy = busy;
        StatusMessage = message ?? string.Empty;
    }

    /// <summary>
    /// Reset the view model to its initial state
    /// </summary>
    protected virtual void Reset()
    {
        IsBusy = false;
        StatusMessage = string.Empty;
    }

    /// <summary>
    /// Validate if the view model is in a valid state
    /// </summary>
    protected virtual bool IsValid => true;
}