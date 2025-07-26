using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Interface for Groq AI service
/// </summary>
public interface IGroqService
{
    /// <summary>
    /// Send a message to Groq and get response
    /// </summary>
    Task<Result<GroqResponse>> SendMessageAsync(
        List<GroqMessage> messages,
        List<GroqFunction>? functions = null,
        CancellationToken cancellationToken = default);
}