using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Orchestrates the chat flow between AI providers and MCP
/// </summary>
public interface IChatOrchestrator
{
    /// <summary>
    /// Process a user message through AI and MCP integration
    /// </summary>
    /// <param name="message">User message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Final response from AI</returns>
    Task<Result<string>> ProcessMessageAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a user message with progress reporting for large operations
    /// </summary>
    /// <param name="message">User message</param>
    /// <param name="progressCallback">Progress callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Final response from AI</returns>
    Task<Result<string>> ProcessMessageWithProgressAsync(string message, IProgress<string>? progressCallback = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the current AI provider
    /// </summary>
    /// <param name="provider">Provider name (Claude, Gemini)</param>
    void SetCurrentProvider(string provider);

    /// <summary>
    /// Get the current AI provider
    /// </summary>
    /// <returns>Current provider name</returns>
    string GetCurrentProvider();
}