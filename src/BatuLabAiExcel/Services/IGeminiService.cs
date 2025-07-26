using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Interface for Gemini AI service
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Send a message to Gemini and get response
    /// </summary>
    Task<Result<GeminiResponse>> SendMessageAsync(
        List<GeminiContent> contents,
        List<GeminiFunctionDeclaration>? functions = null,
        CancellationToken cancellationToken = default);
}