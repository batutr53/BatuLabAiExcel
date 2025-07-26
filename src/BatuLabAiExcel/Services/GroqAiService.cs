using Microsoft.Extensions.Logging;
using System.Text.Json;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Groq AI service adapter implementing unified IAiService interface
/// </summary>
public class GroqAiService : IAiService
{
    private readonly IGroqService _groqService;
    private readonly ILogger<GroqAiService> _logger;

    public string ProviderName => "Groq";

    public GroqAiService(IGroqService groqService, ILogger<GroqAiService> logger)
    {
        _groqService = groqService;
        _logger = logger;
    }

    public async Task<Result<AiResponse>> SendMessageAsync(
        List<AiMessage> messages,
        List<AiTool>? tools = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert unified format to Groq format
            var groqMessages = ConvertToGroqMessages(messages);
            var groqFunctions = tools?.Select(ConvertToGroqFunction).ToList();

            var result = await _groqService.SendMessageAsync(groqMessages, groqFunctions, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return Result<AiResponse>.Failure(result.Error!);
            }

            var aiResponse = ConvertFromGroqResponse(result.Value!);
            return Result<AiResponse>.Success(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Groq AI service adapter");
            return Result<AiResponse>.Failure($"Groq adapter error: {ex.Message}");
        }
    }

    private List<GroqMessage> ConvertToGroqMessages(List<AiMessage> messages)
    {
        var groqMessages = new List<GroqMessage>();

        foreach (var message in messages)
        {
            switch (message.Role.ToLowerInvariant())
            {
                case "user":
                    // For user messages, check if they contain tool results
                    var toolResults = message.Content.Where(c => c.Type == "tool_result").ToList();
                    if (toolResults.Any())
                    {
                        // Add tool result messages
                        foreach (var toolResult in toolResults)
                        {
                            groqMessages.Add(new GroqMessage
                            {
                                Role = "tool",
                                Content = toolResult.ToolResult ?? string.Empty,
                                ToolCallId = toolResult.ToolUseId ?? string.Empty
                            });
                        }
                    }
                    else
                    {
                        // Regular user message
                        var textContent = message.Content
                            .Where(c => c.Type == "text")
                            .Select(c => c.Text)
                            .FirstOrDefault();

                        if (!string.IsNullOrEmpty(textContent))
                        {
                            groqMessages.Add(new GroqMessage
                            {
                                Role = "user",
                                Content = textContent
                            });
                        }
                    }
                    break;

                case "assistant":
                    // For assistant messages, check for tool use
                    var textParts = message.Content.Where(c => c.Type == "text").ToList();
                    var toolUses = message.Content.Where(c => c.Type == "tool_use").ToList();

                    var assistantMessage = new GroqMessage
                    {
                        Role = "assistant"
                    };

                    // Add text content if available
                    if (textParts.Any())
                    {
                        assistantMessage.Content = string.Join(" ", textParts.Select(t => t.Text));
                    }

                    // Add tool calls if available
                    if (toolUses.Any())
                    {
                        assistantMessage.ToolCalls = toolUses.Select(tu => new GroqToolCall
                        {
                            Id = tu.ToolUseId ?? Guid.NewGuid().ToString(),
                            Type = "function",
                            Function = new GroqFunctionCall
                            {
                                Name = tu.ToolName ?? string.Empty,
                                Arguments = JsonSerializer.Serialize(tu.ToolInput ?? new object())
                            }
                        }).ToList();
                    }

                    groqMessages.Add(assistantMessage);
                    break;
            }
        }

        return groqMessages;
    }

    private GroqFunction ConvertToGroqFunction(AiTool tool)
    {
        return new GroqFunction
        {
            Name = tool.Name,
            Description = tool.Description,
            Parameters = new GroqFunctionParameters
            {
                Type = tool.Parameters.Type,
                Properties = tool.Parameters.Properties?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new GroqParameterProperty
                    {
                        Type = ConvertParameterTypeForGroq(kvp.Value.Type),
                        Description = kvp.Value.Description,
                        Enum = kvp.Value.Enum
                    }),
                Required = tool.Parameters.Required
            }
        };
    }

    private string ConvertParameterTypeForGroq(string type)
    {
        // Groq doesn't support complex object types in function parameters
        // Convert object types to string for JSON serialization
        return type switch
        {
            "object" => "string",
            "array" => "string", 
            _ => type
        };
    }

    private AiResponse ConvertFromGroqResponse(GroqResponse groqResponse)
    {
        var aiContent = new List<AiResponseContent>();

        if (groqResponse.Choices.Any())
        {
            var choice = groqResponse.Choices.First();
            var message = choice.Message;

            // Add text content if available
            if (!string.IsNullOrEmpty(message.Content))
            {
                aiContent.Add(new AiResponseContent
                {
                    Type = "text",
                    Text = message.Content
                });
            }

            // Add tool calls if available
            if (message.ToolCalls?.Any() == true)
            {
                foreach (var toolCall in message.ToolCalls)
                {
                    object? parsedArguments = null;
                    try
                    {
                        parsedArguments = JsonSerializer.Deserialize<object>(toolCall.Function.Arguments);
                    }
                    catch
                    {
                        parsedArguments = toolCall.Function.Arguments;
                    }

                    aiContent.Add(new AiResponseContent
                    {
                        Type = "tool_use",
                        ToolUseId = toolCall.Id,
                        ToolName = toolCall.Function.Name,
                        ToolInput = parsedArguments
                    });
                }
            }
        }

        return new AiResponse
        {
            Id = groqResponse.Id,
            Provider = "Groq",
            Content = aiContent,
            Usage = groqResponse.Usage != null ? new AiUsage
            {
                InputTokens = groqResponse.Usage.PromptTokens,
                OutputTokens = groqResponse.Usage.CompletionTokens,
                TotalTokens = groqResponse.Usage.TotalTokens
            } : null,
            FinishReason = groqResponse.Choices.FirstOrDefault()?.FinishReason
        };
    }
}