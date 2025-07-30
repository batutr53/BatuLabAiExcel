using Microsoft.Extensions.Logging;
using BatuLabAiExcel.Models;
using System.Text.Json;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Enhanced adapter to make desktop automation services compatible with unified AI service interface including MCP tool support
/// </summary>
public class DesktopAutomationAiService : IAiService
{
    private readonly IDesktopAutomationService _desktopService;
    private readonly IMcpClient _mcpClient;
    private readonly ILogger<DesktopAutomationAiService> _logger;

    public string ProviderName => _desktopService.ProviderName;

    public DesktopAutomationAiService(
        IDesktopAutomationService desktopService,
        IMcpClient mcpClient,
        ILogger<DesktopAutomationAiService> logger)
    {
        _desktopService = desktopService;
        _mcpClient = mcpClient;
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

            // Ensure MCP client is initialized for Excel tools
            var initResult = await _mcpClient.EnsureInitializedAsync(cancellationToken);
            if (!initResult.IsSuccess)
            {
                _logger.LogWarning("MCP client not available for {Provider}: {Error}", ProviderName, initResult.Error);
                // Continue without MCP tools
            }

            // Desktop apps don't support tools/functions natively, but we can integrate MCP functionality
            var messageText = BuildMessageText(messages, tools);
            
            // Check if the user message is asking for Excel-specific operations
            var needsExcelIntegration = CheckIfNeedsExcelIntegration(messageText);
            
            if (needsExcelIntegration && initResult.IsSuccess)
            {
                // Handle Excel integration by executing MCP tools and then asking ChatGPT to process the results
                return await HandleExcelIntegratedRequestAsync(messageText, tools, cancellationToken);
            }
            else
            {
                // Standard desktop automation without Excel integration
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in desktop automation AI service for {Provider}", ProviderName);
            return Result<AiResponse>.Failure($"Desktop automation error: {ex.Message}");
        }
    }

    private string BuildMessageText(List<AiMessage> messages, List<AiTool>? tools)
    {
        // For ChatGPT Desktop, keep messages simple to avoid "chatgpt" responses
        // The complex context seems to confuse ChatGPT Desktop
        
        // Extract just the user's actual message content
        var userMessages = messages.Where(m => m.Role == "user").ToList();
        if (!userMessages.Any())
        {
            return "";
        }

        // Get the latest user message
        var latestUserMessage = userMessages.Last();
        var userContent = string.Join(" ", latestUserMessage.Content
            .Where(c => c.Type == "text")
            .Select(c => c.Text ?? string.Empty)
            .Where(text => !string.IsNullOrWhiteSpace(text)));

        if (string.IsNullOrWhiteSpace(userContent))
        {
            return "";
        }

        // For ChatGPT Desktop, send just the clean user message without complex context
        // The Excel integration (if needed) should be handled by HandleExcelIntegratedRequestAsync
        var finalMessage = userContent.Trim();
        
        // Ensure reasonable length
        if (finalMessage.Length > 1000)
        {
            finalMessage = finalMessage[..997] + "...";
        }
        
        return finalMessage;
    }

    /// <summary>
    /// Check if the user message requires Excel integration
    /// </summary>
    private bool CheckIfNeedsExcelIntegration(string messageText)
    {
        var excelKeywords = new[]
        {
            "excel", "spreadsheet", "workbook", "worksheet", "sheet", "cells", "cell",
            "data", "read", "write", "incele", "analiz", "veri", "tablo", "satır", "sütun",
            "formula", "chart", "pivot", "format", "a1", "b1", "range", "değer", "hesapla"
        };

        var lowerMessage = messageText.ToLowerInvariant();
        var needsExcel = excelKeywords.Any(keyword => lowerMessage.Contains(keyword));
        
        return needsExcel;
    }

    /// <summary>
    /// Handle Excel-integrated request by first executing MCP tools then asking ChatGPT to process results
    /// </summary>
    private async Task<Result<AiResponse>> HandleExcelIntegratedRequestAsync(
        string messageText, 
        List<AiTool>? tools, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Get Excel context by reading current workbook metadata
            var excelContext = await GetExcelContextAsync(cancellationToken);
            
            // Step 2: Determine what Excel operations to perform based on the message
            var suggestedOperations = DetermineSuggestedOperations(messageText, tools);
            
            // Step 3: Execute the most relevant Excel operations
            var excelResults = new List<string>();
            
            foreach (var operation in suggestedOperations.Take(3)) // Limit to avoid overwhelming ChatGPT
            {
                var toolResult = await _mcpClient.CallToolAsync(operation.ToolName, operation.Arguments, cancellationToken);
                if (toolResult.IsSuccess)
                {
                    var resultText = $"{operation.ToolName}: {toolResult.Value}";
                    excelResults.Add(resultText);
                }
                else
                {
                    var errorText = $"{operation.ToolName}: Error - {toolResult.Error}";
                    excelResults.Add(errorText);
                    _logger.LogWarning("MCP tool {ToolName} failed: {Error}", operation.ToolName, toolResult.Error);
                }
            }

            // Step 4: Build enriched message with Excel data
            var enrichedMessage = BuildEnrichedMessage(messageText, excelContext, excelResults);
            
            // Step 5: Send to ChatGPT Desktop
            var result = await _desktopService.SendMessageAsync(enrichedMessage, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return Result<AiResponse>.Failure(result.Error!);
            }

            // Step 6: Return response
            var aiResponse = new AiResponse
            {
                Id = Guid.NewGuid().ToString(),
                Provider = $"{ProviderName} + Excel",
                Content = new List<AiResponseContent>
                {
                    new AiResponseContent
                    {
                        Type = "text",
                        Text = result.Value!
                    }
                },
                Usage = null,
                FinishReason = "stop"
            };

            return Result<AiResponse>.Success(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Excel-integrated request for {Provider}", ProviderName);
            return Result<AiResponse>.Failure($"Excel integration error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get current Excel workbook context
    /// </summary>
    private async Task<string> GetExcelContextAsync(CancellationToken cancellationToken)
    {
        try
        {
            var metadataResult = await _mcpClient.CallToolAsync("get_workbook_metadata", new { }, cancellationToken);
            return metadataResult.IsSuccess ? metadataResult.Value! : "No Excel context available";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Excel context");
            return "Excel context unavailable";
        }
    }

    /// <summary>
    /// Determine what Excel operations to perform based on user message
    /// </summary>
    private List<(string ToolName, object Arguments)> DetermineSuggestedOperations(string messageText, List<AiTool>? tools)
    {
        var operations = new List<(string ToolName, object Arguments)>();
        var lowerMessage = messageText.ToLowerInvariant();

        // Basic operations based on keywords
        if (lowerMessage.Contains("incele") || lowerMessage.Contains("analiz") || lowerMessage.Contains("read") || lowerMessage.Contains("oku"))
        {
            operations.Add(("get_workbook_metadata", new { include_ranges = true }));
            operations.Add(("read_data_from_excel", new { filepath = "", sheet_name = "Sheet1", start_cell = "A1", end_cell = "J20" }));
        }

        if (lowerMessage.Contains("sheet") || lowerMessage.Contains("worksheet") || lowerMessage.Contains("sayfa"))
        {
            operations.Add(("get_workbook_metadata", new { include_ranges = true }));
        }

        // If no specific operations determined, default to reading workbook metadata
        if (!operations.Any())
        {
            operations.Add(("get_workbook_metadata", new { include_ranges = true }));
        }

        return operations;
    }

    /// <summary>
    /// Build enriched message with Excel context and data
    /// </summary>
    private string BuildEnrichedMessage(string originalMessage, string excelContext, List<string> excelResults)
    {
        var enrichedBuilder = new List<string>
        {
            "EXCEL DATA CONTEXT:",
            "==================",
            excelContext,
            "",
            "EXCEL OPERATION RESULTS:",
            "========================"
        };

        enrichedBuilder.AddRange(excelResults);
        
        enrichedBuilder.AddRange(new[]
        {
            "",
            "USER REQUEST:",
            "=============",
            originalMessage,
            "",
            "Please analyze the Excel data above and respond to the user's request. Provide specific insights based on the actual data shown."
        });

        return string.Join("\n", enrichedBuilder);
    }
}