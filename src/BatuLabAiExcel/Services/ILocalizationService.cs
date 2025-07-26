using System.ComponentModel;
using System.Globalization;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Service for managing application localization
/// </summary>
public interface ILocalizationService : INotifyPropertyChanged
{
    /// <summary>
    /// Current culture
    /// </summary>
    CultureInfo CurrentCulture { get; }
    
    /// <summary>
    /// Available cultures
    /// </summary>
    IEnumerable<CultureInfo> AvailableCultures { get; }
    
    /// <summary>
    /// Change the current culture
    /// </summary>
    void SetCulture(string cultureName);
    
    /// <summary>
    /// Get localized string by key
    /// </summary>
    string GetString(string key);
    
    /// <summary>
    /// Get localized string with format parameters
    /// </summary>
    string GetString(string key, params object[] args);
    
    /// <summary>
    /// Event fired when culture changes
    /// </summary>
    event EventHandler<CultureInfo>? CultureChanged;
}