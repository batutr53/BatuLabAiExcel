using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Service for interacting with Claude API
/// </summary>
public interface IClaudeService
{
    /// <summary>
    /// Send a message to Claude with optional tools
    /// </summary>
    /// <param name="messages">Conversation messages</param>
    /// <param name="tools">Available tools</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Claude's response</returns>
    Task<Result<ClaudeResponse>> SendMessageAsync(
        List<ClaudeMessage> messages,
        List<ClaudeTool>? tools = null,
        CancellationToken cancellationToken = default);
}