using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Orchestrates the chat flow between Claude and MCP
/// </summary>
public interface IChatOrchestrator
{
    /// <summary>
    /// Process a user message through Claude and MCP integration
    /// </summary>
    /// <param name="message">User message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Final response from Claude</returns>
    Task<Result<string>> ProcessMessageAsync(string message, CancellationToken cancellationToken = default);
}