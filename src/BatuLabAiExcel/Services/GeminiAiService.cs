using Microsoft.Extensions.Logging;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Gemini AI service adapter implementing unified IAiService interface
/// </summary>
public class GeminiAiService : IAiService
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<GeminiAiService> _logger;

    public string ProviderName => "Gemini";

    public GeminiAiService(IGeminiService geminiService, ILogger<GeminiAiService> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<Result<AiResponse>> SendMessageAsync(
        List<AiMessage> messages,
        List<AiTool>? tools = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert unified format to Gemini format
            var geminiContents = ConvertToGeminiContents(messages);
            var geminiFunctions = tools?.Select(ConvertToGeminiFunction).ToList();

            var result = await _geminiService.SendMessageAsync(geminiContents, geminiFunctions, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return Result<AiResponse>.Failure(result.Error!);
            }

            var aiResponse = ConvertFromGeminiResponse(result.Value!);
            return Result<AiResponse>.Success(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Gemini AI service adapter");
            return Result<AiResponse>.Failure($"Gemini adapter error: {ex.Message}");
        }
    }

    private List<GeminiContent> ConvertToGeminiContents(List<AiMessage> messages)
    {
        var geminiContents = new List<GeminiContent>();

        foreach (var message in messages)
        {
            var parts = new List<GeminiPart>();

            foreach (var content in message.Content)
            {
                switch (content.Type)
                {
                    case "text":
                        parts.Add(new GeminiPart
                        {
                            Text = content.Text ?? string.Empty
                        });
                        break;

                    case "tool_result":
                        parts.Add(new GeminiPart
                        {
                            FunctionResponse = new GeminiFunctionResponse
                            {
                                Name = content.ToolName ?? string.Empty,
                                Response = new { result = content.ToolResult, isError = content.IsError ?? false }
                            }
                        });
                        break;
                }
            }

            geminiContents.Add(new GeminiContent
            {
                Role = ConvertRoleToGemini(message.Role),
                Parts = parts
            });
        }

        return geminiContents;
    }

    private string? ConvertRoleToGemini(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "user" => "user",
            "assistant" => "model",
            _ => null
        };
    }

    private GeminiFunctionDeclaration ConvertToGeminiFunction(AiTool tool)
    {
        return new GeminiFunctionDeclaration
        {
            Name = tool.Name,
            Description = tool.Description,
            Parameters = new GeminiSchema
            {
                Type = tool.Parameters.Type,
                Properties = tool.Parameters.Properties?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new GeminiSchemaProperty
                    {
                        Type = kvp.Value.Type,
                        Description = kvp.Value.Description,
                        Enum = kvp.Value.Enum
                    }),
                Required = tool.Parameters.Required
            }
        };
    }

    private AiResponse ConvertFromGeminiResponse(GeminiResponse geminiResponse)
    {
        var aiContent = new List<AiResponseContent>();
        var responseId = Guid.NewGuid().ToString();

        if (geminiResponse.Candidates.Any())
        {
            var candidate = geminiResponse.Candidates.First();
            
            foreach (var part in candidate.Content.Parts)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    aiContent.Add(new AiResponseContent
                    {
                        Type = "text",
                        Text = part.Text
                    });
                }

                if (part.FunctionCall != null)
                {
                    aiContent.Add(new AiResponseContent
                    {
                        Type = "tool_use",
                        ToolUseId = Guid.NewGuid().ToString(),
                        ToolName = part.FunctionCall.Name,
                        ToolInput = part.FunctionCall.Args
                    });
                }
            }
        }

        return new AiResponse
        {
            Id = responseId,
            Provider = "Gemini",
            Content = aiContent,
            Usage = geminiResponse.UsageMetadata != null ? new AiUsage
            {
                InputTokens = geminiResponse.UsageMetadata.PromptTokenCount,
                OutputTokens = geminiResponse.UsageMetadata.CandidatesTokenCount,
                TotalTokens = geminiResponse.UsageMetadata.TotalTokenCount
            } : null,
            FinishReason = geminiResponse.Candidates.FirstOrDefault()?.FinishReason
        };
    }
}