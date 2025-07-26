using Microsoft.Extensions.Logging;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Adapter to make desktop automation services compatible with unified AI service interface
/// </summary>
public class DesktopAutomationAiService : IAiService
{
    private readonly IDesktopAutomationService _desktopService;
    private readonly ILogger<DesktopAutomationAiService> _logger;

    public string ProviderName => _desktopService.ProviderName;

    public DesktopAutomationAiService(
        IDesktopAutomationService desktopService,
        ILogger<DesktopAutomationAiService> logger)
    {
        _desktopService = desktopService;
        _logger = logger;
    }

    public async Task<Result<AiResponse>> SendMessageAsync(
        List<AiMessage> messages,
        List<AiTool>? tools = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_desktopService.IsEnabled)
            {
                return Result<AiResponse>.Failure($"{ProviderName} is disabled in configuration");
            }

            // Desktop apps don't support tools/functions, so we'll convert the message to include tool context
            var messageText = BuildMessageText(messages, tools);
            
            var result = await _desktopService.SendMessageAsync(messageText, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return Result<AiResponse>.Failure(result.Error!);
            }

            var aiResponse = new AiResponse
            {
                Id = Guid.NewGuid().ToString(),
                Provider = ProviderName,
                Content = new List<AiResponseContent>
                {
                    new AiResponseContent
                    {
                        Type = "text",
                        Text = result.Value!
                    }
                },
                Usage = null, // Desktop apps don't provide token usage
                FinishReason = "stop"
            };

            return Result<AiResponse>.Success(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in desktop automation AI service");
            return Result<AiResponse>.Failure($"Desktop automation error: {ex.Message}");
        }
    }

    private string BuildMessageText(List<AiMessage> messages, List<AiTool>? tools)
    {
        var messageBuilder = new List<string>();

        // Add context about Excel integration if tools are available
        if (tools?.Any() == true)
        {
            messageBuilder.Add("CONTEXT: I am working with Excel files and have access to these functions:");
            foreach (var tool in tools.Take(5)) // Limit to avoid overwhelming the desktop app
            {
                messageBuilder.Add($"- {tool.Name}: {tool.Description}");
            }
            messageBuilder.Add("");
        }

        // Add conversation history (last few messages to maintain context)
        var recentMessages = messages.TakeLast(3).ToList();
        
        foreach (var message in recentMessages)
        {
            var content = string.Join(" ", message.Content
                .Where(c => c.Type == "text")
                .Select(c => c.Text ?? string.Empty));

            if (!string.IsNullOrEmpty(content))
            {
                var roleLabel = message.Role switch
                {
                    "user" => "USER",
                    "assistant" => "ASSISTANT",
                    _ => message.Role.ToUpper()
                };

                messageBuilder.Add($"{roleLabel}: {content}");
            }
        }

        // Add Excel context if we have tool results
        var toolResults = messages.SelectMany(m => m.Content)
            .Where(c => c.Type == "tool_result")
            .ToList();

        if (toolResults.Any())
        {
            messageBuilder.Add("");
            messageBuilder.Add("EXCEL DATA CONTEXT:");
            foreach (var result in toolResults.TakeLast(3))
            {
                if (!string.IsNullOrEmpty(result.ToolResult))
                {
                    var truncatedResult = result.ToolResult.Length > 200 
                        ? result.ToolResult[..200] + "..." 
                        : result.ToolResult;
                    messageBuilder.Add($"- {result.ToolUseId}: {truncatedResult}");
                }
            }
        }

        var finalMessage = string.Join("\n", messageBuilder);
        
        // Ensure the message isn't too long for desktop apps
        if (finalMessage.Length > 2000)
        {
            finalMessage = finalMessage[..1997] + "...";
        }

        return finalMessage;
    }
}