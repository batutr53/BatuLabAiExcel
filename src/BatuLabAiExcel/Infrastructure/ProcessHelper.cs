using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BatuLabAiExcel.Infrastructure;

/// <summary>
/// Helper class for process management
/// </summary>
public class ProcessHelper
{
    private readonly ILogger<ProcessHelper> _logger;

    public ProcessHelper(ILogger<ProcessHelper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Safely kill a process and its children
    /// </summary>
    /// <param name="process">Process to kill</param>
    public void SafeKillProcess(Process process)
    {
        if (process == null || process.HasExited)
        {
            return;
        }

        try
        {
            _logger.LogDebug("Terminating process {ProcessId}", process.Id);

            // First try graceful termination
            if (!process.HasExited)
            {
                process.CloseMainWindow();
                
                // Wait a bit for graceful shutdown
                if (!process.WaitForExit(3000))
                {
                    // Force kill if graceful shutdown failed
                    _logger.LogWarning("Force killing process {ProcessId}", process.Id);
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
            }

            _logger.LogDebug("Process {ProcessId} terminated", process.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error killing process {ProcessId}", process.Id);
        }
        finally
        {
            try
            {
                process.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing process");
            }
        }
    }

    /// <summary>
    /// Check if a command is available in PATH
    /// </summary>
    /// <param name="command">Command to check</param>
    /// <returns>True if command is available</returns>
    public bool IsCommandAvailable(string command)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "where", // Windows command to find executable
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return false;
            }

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get environment variable with fallback
    /// </summary>
    /// <param name="variableName">Environment variable name</param>
    /// <param name="fallback">Fallback value</param>
    /// <returns>Environment variable value or fallback</returns>
    public string GetEnvironmentVariable(string variableName, string fallback)
    {
        return Environment.GetEnvironmentVariable(variableName) ?? fallback;
    }
}