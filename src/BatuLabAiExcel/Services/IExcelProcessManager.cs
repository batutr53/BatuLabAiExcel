using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Service for managing Excel process interactions
/// </summary>
public interface IExcelProcessManager
{
    /// <summary>
    /// Check if Excel file is currently open
    /// </summary>
    Task<Result<bool>> IsExcelFileOpenAsync(string filePath);
    
    /// <summary>
    /// Close Excel file if it's open
    /// </summary>
    Task<Result> CloseExcelFileAsync(string filePath);
    
    /// <summary>
    /// Open Excel file after processing
    /// </summary>
    Task<Result> OpenExcelFileAsync(string filePath);
    
    /// <summary>
    /// Get list of processes that might have the file locked
    /// </summary>
    Task<Result<List<string>>> GetProcessesUsingFileAsync(string filePath);
}