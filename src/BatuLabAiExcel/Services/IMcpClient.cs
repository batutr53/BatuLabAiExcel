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
    /// Check if the MCP server is running and responsive
    /// </summary>
    Task<Result> CheckHealthAsync(CancellationToken cancellationToken = default);
}