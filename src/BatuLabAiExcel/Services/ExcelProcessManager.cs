using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Extensions.Logging;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Manages Excel process interactions for file locking/unlocking
/// </summary>
public class ExcelProcessManager : IExcelProcessManager
{
    private readonly ILogger<ExcelProcessManager> _logger;

    public ExcelProcessManager(ILogger<ExcelProcessManager> logger)
    {
        _logger = logger;
    }

    public async Task<Result<bool>> IsExcelFileOpenAsync(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            _logger.LogDebug("Checking if Excel file is open: {FileName}", fileName);

            // Check if file is locked by trying to open it exclusively
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                // If we can open it exclusively, it's not locked
                _logger.LogDebug("File is not locked: {FileName}", fileName);
                return Result<bool>.Success(false);
            }
            catch (IOException)
            {
                // File is locked, check if it's by Excel
                var processes = await GetProcessesUsingFileAsync(filePath);
                if (processes.IsSuccess)
                {
                    var excelProcesses = processes.Value!.Where(p => 
                        p.Contains("excel", StringComparison.OrdinalIgnoreCase) ||
                        p.Contains("EXCEL", StringComparison.OrdinalIgnoreCase)).ToList();
                    
                    var isExcelOpen = excelProcesses.Any();
                    _logger.LogInformation("File {FileName} is locked by Excel: {IsLocked}", fileName, isExcelOpen);
                    return Result<bool>.Success(isExcelOpen);
                }
                
                // Assume it's locked if we can't determine
                _logger.LogWarning("File {FileName} appears to be locked but couldn't determine by which process", fileName);
                return Result<bool>.Success(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if Excel file is open: {FilePath}", filePath);
            return Result<bool>.Failure($"Error checking file status: {ex.Message}");
        }
    }

    public async Task<Result> CloseExcelFileAsync(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            _logger.LogInformation("Attempting to close Excel file: {FileName}", fileName);

            // Get Excel processes that might have this file open
            var processes = Process.GetProcessesByName("EXCEL");
            var closedAny = false;

            foreach (var process in processes)
            {
                try
                {
                    // Try to get the window title or command line to match the file
                    var windowTitle = process.MainWindowTitle;
                    
                    if (!string.IsNullOrEmpty(windowTitle) && 
                        (windowTitle.Contains(fileName, StringComparison.OrdinalIgnoreCase) ||
                         windowTitle.Contains(Path.GetFileNameWithoutExtension(fileName), StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.LogInformation("Closing Excel process: {ProcessId} - {WindowTitle}", process.Id, windowTitle);
                        
                        // Send WM_CLOSE message to gracefully close
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            SendMessage(process.MainWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                            await Task.Delay(2000); // Wait for graceful close
                            
                            if (!process.HasExited)
                            {
                                process.CloseMainWindow();
                                await Task.Delay(1000);
                                
                                if (!process.HasExited)
                                {
                                    _logger.LogWarning("Force killing Excel process: {ProcessId}", process.Id);
                                    process.Kill();
                                }
                            }
                        }
                        else
                        {
                            process.Kill();
                        }
                        
                        closedAny = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing Excel process: {ProcessId}", process.Id);
                }
                finally
                {
                    process.Dispose();
                }
            }

            if (closedAny)
            {
                // Wait a bit for the file to be unlocked
                await Task.Delay(1000);
                _logger.LogInformation("Excel file closed: {FileName}", fileName);
            }
            else
            {
                _logger.LogInformation("No Excel processes found for file: {FileName}", fileName);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing Excel file: {FilePath}", filePath);
            return Result.Failure($"Error closing Excel file: {ex.Message}");
        }
    }

    public async Task<Result> OpenExcelFileAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Opening Excel file: {FilePath}", filePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

            var process = Process.Start(startInfo);
            if (process != null)
            {
                _logger.LogInformation("Excel file opened successfully: {FilePath}", filePath);
                return Result.Success();
            }
            else
            {
                return Result.Failure("Failed to start Excel process");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening Excel file: {FilePath}", filePath);
            return Result.Failure($"Error opening Excel file: {ex.Message}");
        }
    }

    public async Task<Result<List<string>>> GetProcessesUsingFileAsync(string filePath)
    {
        try
        {
            var processes = new List<string>();
            
            // Simple approach: get all Excel processes
            var excelProcesses = Process.GetProcessesByName("EXCEL");
            foreach (var process in excelProcesses)
            {
                try
                {
                    var processInfo = $"Excel (PID: {process.Id})";
                    if (!string.IsNullOrEmpty(process.MainWindowTitle))
                    {
                        processInfo += $" - {process.MainWindowTitle}";
                    }
                    processes.Add(processInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Error getting process info for Excel PID: {ProcessId}", process.Id);
                }
                finally
                {
                    process.Dispose();
                }
            }

            return Result<List<string>>.Success(processes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting processes using file: {FilePath}", filePath);
            return Result<List<string>>.Failure($"Error getting processes: {ex.Message}");
        }
    }

    // Windows API for sending close message
    private const int WM_CLOSE = 0x0010;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}