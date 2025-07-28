using BatuLabAiExcel.WebApi.Models;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// Service for managing admin settings
/// </summary>
public interface IAdminSettingsService
{
    /// <summary>
    /// Get all admin settings
    /// </summary>
    Task<ApiResponse<AdminSettings>> GetSettingsAsync();

    /// <summary>
    /// Update admin settings
    /// </summary>
    Task<ApiResponse<AdminSettings>> UpdateSettingsAsync(AdminSettings settings);
}