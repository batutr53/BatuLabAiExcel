using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Client for communicating with MCP server via JSON-RPC over stdio
/// </summary>
public interface IMcpClient : IDisposable
{
    /// <summary>
    /// Ensure the MCP client is initialized and connected
    /// </summary>
    Task<Result> EnsureInitializedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available tools from the MCP server
    /// </summary>
    Task<Result<List<McpTool>>> GetAvailableToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Call a tool on the MCP server
    /// </summary>
    /// <param name="toolName">Name of the tool to call</param>
    /// <param name="arguments">Tool arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool result</returns>
    Task<Result<string>> CallToolAsync(string toolName, object? arguments, CancellationToken cancellationToken = default);

    /// <summary>
    /// Call multiple tools in parallel for better performance
    /// </summary>
    /// <param name="toolCalls">List of tool calls to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of results keyed by request ID</returns>
    Task<Result<Dictionary<string, string>>> CallToolsBatchAsync(List<(string id, string toolName, object? arguments)> toolCalls, CancellationToken cancellationToken = default);

    /// <summary>
    /// Call a tool with progress reporting for large operations
    /// </summary>
    /// <param name="toolName">Name of the tool to call</param>
    /// <param name="arguments">Tool arguments</param>
    /// <param name="progressCallback">Progress callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool result</returns>
    Task<Result<string>> CallToolWithProgressAsync(string toolName, object? arguments, IProgress<string>? progressCallback = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the MCP server is running and responsive
    /// </summary>
    Task<Result> CheckHealthAsync(CancellationToken cancellationToken = default);
}