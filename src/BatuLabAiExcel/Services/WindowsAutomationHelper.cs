using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Helper class for Windows UI Automation and Win32 API interactions
/// </summary>
public class WindowsAutomationHelper
{
    private readonly ILogger<WindowsAutomationHelper> _logger;

    // P/Invoke declarations
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const int KEYEVENTF_KEYUP = 0x02;
    private const byte VK_CONTROL = 0x11;
    private const byte VK_A = 0x41;
    private const byte VK_V = 0x56;
    private const byte VK_RETURN = 0x0D;

    public WindowsAutomationHelper(ILogger<WindowsAutomationHelper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Find a window by process name and/or window title
    /// </summary>
    public Task<IntPtr> FindWindowAsync(string? processName = null, string? windowTitle = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(processName))
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    var process = processes.First();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        _logger.LogDebug("Found window handle {Handle} for process {Process}", 
                            process.MainWindowHandle, processName);
                        return Task.FromResult(process.MainWindowHandle);
                    }
                }
            }

            if (!string.IsNullOrEmpty(windowTitle))
            {
                var handle = FindWindow(null, windowTitle);
                if (handle != IntPtr.Zero)
                {
                    _logger.LogDebug("Found window handle {Handle} for title {Title}", 
                        handle, windowTitle);
                    return Task.FromResult(handle);
                }
            }

            return Task.FromResult(IntPtr.Zero);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding window for process {Process}, title {Title}", 
                processName, windowTitle);
            return Task.FromResult(IntPtr.Zero);
        }
    }

    /// <summary>
    /// Check if a process is running
    /// </summary>
    public bool IsProcessRunning(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if process {Process} is running", processName);
            return false;
        }
    }

    /// <summary>
    /// Launch an application using various methods
    /// </summary>
    public async Task<Process?> LaunchApplicationAsync(string launchPath, string? launchCommand = null)
    {
        try
        {
            // Try protocol handler first (e.g., claude://, chatgpt://)
            if (!string.IsNullOrEmpty(launchCommand) && launchCommand.Contains("://"))
            {
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = launchCommand,
                        UseShellExecute = true
                    };
                    
                    var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        _logger.LogInformation("Launched application using protocol: {Command}", launchCommand);
                        return process;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to launch using protocol {Command}, trying direct path", launchCommand);
                }
            }

            // Try direct executable path
            if (!string.IsNullOrEmpty(launchPath))
            {
                var expandedPath = Environment.ExpandEnvironmentVariables(launchPath);
                if (File.Exists(expandedPath))
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = expandedPath,
                        UseShellExecute = true
                    };
                    
                    var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        _logger.LogInformation("Launched application from path: {Path}", expandedPath);
                        return process;
                    }
                }
                else
                {
                    _logger.LogWarning("Launch path does not exist: {Path}", expandedPath);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error launching application with path {Path}, command {Command}", 
                launchPath, launchCommand);
            return null;
        }
    }

    /// <summary>
    /// Send text to a window using Win32 API (keyboard simulation only)
    /// </summary>
    public async Task<bool> SendTextToWindowAsync(IntPtr windowHandle, string text)
    {
        try
        {
            if (windowHandle == IntPtr.Zero)
            {
                _logger.LogWarning("Invalid window handle for sending text");
                return false;
            }

            // Focus the window first
            SetForegroundWindow(windowHandle);
            await Task.Delay(1000); // Wait for focus

            // Clear any existing content first (Ctrl+A)
            SendKeyDown(VK_CONTROL);
            await Task.Delay(50);
            SendKeyPress(VK_A);
            await Task.Delay(50);
            SendKeyUp(VK_CONTROL);
            await Task.Delay(100);

            // Send the text character by character using Unicode input
            await SendTextByKeyboard(text);

            _logger.LogDebug("Sent text to window {Handle}: {Text}", windowHandle, text.Length > 50 ? text[..50] + "..." : text);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending text to window {Handle}", windowHandle);
            return false;
        }
    }

    private async Task SendTextByKeyboard(string text)
    {
        foreach (char c in text)
        {
            if (char.IsControl(c))
            {
                // Handle special characters
                switch (c)
                {
                    case '\n':
                    case '\r':
                        SendKeyPress(VK_RETURN);
                        break;
                    case '\t':
                        SendKeyPress(0x09); // Tab
                        break;
                    default:
                        SendKeyPress(0x20); // Space for other control chars
                        break;
                }
            }
            else
            {
                // Send Unicode character
                SendUnicodeChar(c);
            }
            
            await Task.Delay(10); // Small delay between characters
        }
    }

    private void SendUnicodeChar(char c)
    {
        const uint INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_UNICODE = 0x0004;
        const uint KEYEVENTF_KEYUP = 0x0002;

        var inputs = new INPUT[2];
        
        // Key down
        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            data = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // Key up
        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            data = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendKeyDown(byte virtualKey)
    {
        keybd_event(virtualKey, 0, 0, IntPtr.Zero);
    }

    private void SendKeyUp(byte virtualKey)
    {
        keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
    }

    private void SendKeyPress(byte virtualKey)
    {
        SendKeyDown(virtualKey);
        SendKeyUp(virtualKey);
    }

    /// <summary>
    /// Send Enter key to a window
    /// </summary>
    public async Task<bool> SendEnterKeyAsync(IntPtr windowHandle)
    {
        try
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            SetForegroundWindow(windowHandle);
            await Task.Delay(100);

            keybd_event(VK_RETURN, 0, 0, IntPtr.Zero); // Enter key down
            keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, IntPtr.Zero); // Enter key up
            
            _logger.LogDebug("Sent Enter key to window {Handle}", windowHandle);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Enter key to window {Handle}", windowHandle);
            return false;
        }
    }

    /// <summary>
    /// Get window text content (basic implementation)
    /// </summary>
    public Task<string> GetWindowTextAsync(IntPtr windowHandle)
    {
        try
        {
            if (windowHandle == IntPtr.Zero)
            {
                return Task.FromResult(string.Empty);
            }

            const int maxLength = 65536;
            var buffer = new StringBuilder(maxLength);
            GetWindowText(windowHandle, buffer, maxLength);
            
            var result = buffer.ToString();
            _logger.LogDebug("Retrieved window text from {Handle}: {Length} characters", 
                windowHandle, result.Length);
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting window text from {Handle}", windowHandle);
            return Task.FromResult(string.Empty);
        }
    }
}