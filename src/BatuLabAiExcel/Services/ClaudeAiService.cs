using Microsoft.Extensions.Logging;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Claude AI service adapter implementing unified IAiService interface
/// </summary>
public class ClaudeAiService : IAiService
{
    private readonly IClaudeService _claudeService;
    private readonly ILogger<ClaudeAiService> _logger;

    public string ProviderName => "Claude";

    public ClaudeAiService(IClaudeService claudeService, ILogger<ClaudeAiService> logger)
    {
        _claudeService = claudeService;
        _logger = logger;
    }

    public async Task<Result<AiResponse>> SendMessageAsync(
        List<AiMessage> messages,
        List<AiTool>? tools = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert unified format to Claude format
            var claudeMessages = ConvertToClaudeMessages(messages);
            var claudeTools = tools?.Select(ConvertToClaudeTool).ToList();

            var result = await _claudeService.SendMessageAsync(claudeMessages, claudeTools, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return Result<AiResponse>.Failure(result.Error!);
            }

            var aiResponse = ConvertFromClaudeResponse(result.Value!);
            return Result<AiResponse>.Success(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Claude AI service adapter");
            return Result<AiResponse>.Failure($"Claude adapter error: {ex.Message}");
        }
    }

    private List<ClaudeMessage> ConvertToClaudeMessages(List<AiMessage> messages)
    {
        var claudeMessages = new List<ClaudeMessage>();

        foreach (var message in messages)
        {
            var claudeContent = new List<object>();

            foreach (var content in message.Content)
            {
                switch (content.Type)
                {
                    case "text":
                        claudeContent.Add(new ClaudeTextContent
                        {
                            Type = "text",
                            Text = content.Text ?? string.Empty
                        });
                        break;

                    case "tool_result":
                        claudeContent.Add(new ClaudeToolResultContent
                        {
                            Type = "tool_result",
                            ToolUseId = content.ToolUseId ?? string.Empty,
                            Content = content.ToolResult ?? string.Empty,
                            IsError = content.IsError
                        });
                        break;

                    case "tool_use":
                        // Convert tool_use from unified format back to Claude format
                        claudeContent.Add(new ClaudeToolUseContent
                        {
                            Type = "tool_use",
                            Id = content.ToolUseId ?? string.Empty,
                            Name = content.ToolName ?? string.Empty,
                            Input = content.ToolInput ?? new object()
                        });
                        break;
                }
            }

            claudeMessages.Add(new ClaudeMessage
            {
                Role = message.Role,
                Content = claudeContent
            });
        }

        return claudeMessages;
    }

    private ClaudeTool ConvertToClaudeTool(AiTool tool)
    {
        return new ClaudeTool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = new ClaudeToolSchema
            {
                Type = tool.Parameters.Type,
                Properties = tool.Parameters.Properties?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ClaudeSchemaProperty
                    {
                        Type = kvp.Value.Type,
                        Description = kvp.Value.Description,
                        Enum = kvp.Value.Enum
                    }),
                Required = tool.Parameters.Required,
                AdditionalProperties = false
            }
        };
    }

    private AiResponse ConvertFromClaudeResponse(ClaudeResponse claudeResponse)
    {
        var aiContent = new List<AiResponseContent>();

        foreach (var content in claudeResponse.Content)
        {
            var aiResponseContent = new AiResponseContent
            {
                Type = content.Type,
                Text = content.Text,
                ToolUseId = content.Id,
                ToolName = content.Name,
                ToolInput = content.Input
            };

            aiContent.Add(aiResponseContent);
        }

        return new AiResponse
        {
            Id = claudeResponse.Id,
            Provider = "Claude",
            Content = aiContent,
            Usage = claudeResponse.Usage != null ? new AiUsage
            {
                InputTokens = claudeResponse.Usage.InputTokens,
                OutputTokens = claudeResponse.Usage.OutputTokens,
                TotalTokens = claudeResponse.Usage.InputTokens + claudeResponse.Usage.OutputTokens
            } : null,
            FinishReason = claudeResponse.StopReason
        };
    }
}