using Microsoft.Extensions.Logging;
using System.Text.Json;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Orchestrates the chat flow between Claude and MCP
/// </summary>
public class ChatOrchestrator : IChatOrchestrator
{
    private readonly IClaudeService _claudeService;
    private readonly IMcpClient _mcpClient;
    private readonly ILogger<ChatOrchestrator> _logger;

    private readonly List<ClaudeMessage> _conversationHistory = new();

    public ChatOrchestrator(
        IClaudeService claudeService,
        IMcpClient mcpClient,
        ILogger<ChatOrchestrator> logger)
    {
        _claudeService = claudeService;
        _mcpClient = mcpClient;
        _logger = logger;
    }

    public async Task<Result<string>> ProcessMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting message processing: {Message}", message);

            // Ensure MCP client is initialized
            var initResult = await _mcpClient.EnsureInitializedAsync(cancellationToken);
            if (!initResult.IsSuccess)
            {
                _logger.LogError("MCP client initialization failed: {Error}", initResult.Error);
                return Result<string>.Failure($"Excel integration not available: {initResult.Error}");
            }

            // Get available tools from MCP
            var toolsResult = await _mcpClient.GetAvailableToolsAsync(cancellationToken);
            if (!toolsResult.IsSuccess)
            {
                _logger.LogWarning("Could not retrieve MCP tools: {Error}", toolsResult.Error);
                // Continue without tools, Claude might still be able to help
            }

            // Add user message to conversation history
            _conversationHistory.Add(new ClaudeMessage
            {
                Role = "user",
                Content = message
            });

            // Prepare Claude request with tools
            var claudeTools = toolsResult.IsSuccess ? ConvertMcpToolsToClaudeTools(toolsResult.Value!) : null;

            var maxRounds = 10; // Prevent infinite loops
            var currentRound = 0;

            while (currentRound < maxRounds)
            {
                currentRound++;
                _logger.LogDebug("Processing round {Round}/{MaxRounds}", currentRound, maxRounds);

                // Send request to Claude
                var claudeResult = await _claudeService.SendMessageAsync(
                    _conversationHistory.ToList(), // Make a copy
                    claudeTools,
                    cancellationToken);

                if (!claudeResult.IsSuccess)
                {
                    _logger.LogError("Claude request failed: {Error}", claudeResult.Error);
                    return Result<string>.Failure($"AI service error: {claudeResult.Error}");
                }

                var response = claudeResult.Value!;
                
                // Add Claude's response to conversation history
                _conversationHistory.Add(new ClaudeMessage
                {
                    Role = "assistant",
                    Content = response.Content
                });

                // Check if Claude wants to use tools
                var toolUses = response.Content
                    .Where(block => block.Type == "tool_use")
                    .ToList();

                if (!toolUses.Any())
                {
                    // No tool use, return the text response
                    var textContent = response.Content
                        .Where(block => block.Type == "text")
                        .Select(block => block.Text)
                        .Where(text => !string.IsNullOrEmpty(text))
                        .FirstOrDefault();

                    _logger.LogInformation("Completed processing in {Rounds} rounds", currentRound);
                    return Result<string>.Success(textContent ?? "No response from AI.");
                }

                // Process tool uses
                var toolResults = new List<ClaudeToolResultContent>();

                foreach (var toolUse in toolUses)
                {
                    if (string.IsNullOrEmpty(toolUse.Id) || string.IsNullOrEmpty(toolUse.Name))
                    {
                        _logger.LogWarning("Invalid tool use: missing ID or name");
                        continue;
                    }

                    _logger.LogInformation("Executing tool: {ToolName} with ID: {ToolId}", toolUse.Name, toolUse.Id);

                    try
                    {
                        // Call MCP tool
                        var mcpResult = await _mcpClient.CallToolAsync(
                            toolUse.Name,
                            toolUse.Input,
                            cancellationToken);

                        var toolResult = new ClaudeToolResultContent
                        {
                            ToolUseId = toolUse.Id,
                            Content = mcpResult.IsSuccess 
                                ? (mcpResult.Value ?? "Tool executed successfully")
                                : $"Error: {mcpResult.Error}",
                            IsError = !mcpResult.IsSuccess
                        };

                        toolResults.Add(toolResult);

                        _logger.LogInformation("Tool {ToolName} executed with result: {Success}", 
                            toolUse.Name, mcpResult.IsSuccess ? "Success" : "Error");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception executing tool {ToolName}", toolUse.Name);
                        
                        toolResults.Add(new ClaudeToolResultContent
                        {
                            ToolUseId = toolUse.Id,
                            Content = $"Tool execution failed: {ex.Message}",
                            IsError = true
                        });
                    }
                }

                // Add tool results to conversation history
                if (toolResults.Any())
                {
                    _conversationHistory.Add(new ClaudeMessage
                    {
                        Role = "user",
                        Content = toolResults.Cast<object>().ToList()
                    });
                }

                // Continue the loop to get Claude's response to the tool results
            }

            _logger.LogWarning("Maximum rounds ({MaxRounds}) reached without completion", maxRounds);
            return Result<string>.Failure("Conversation exceeded maximum rounds. Please try a simpler request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message");
            return Result<string>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    private List<ClaudeTool> ConvertMcpToolsToClaudeTools(List<McpTool> mcpTools)
    {
        return mcpTools.Select(mcpTool => new ClaudeTool
        {
            Name = mcpTool.Name,
            Description = mcpTool.Description,
            InputSchema = new ClaudeToolSchema
            {
                Type = mcpTool.InputSchema.Type,
                Properties = mcpTool.InputSchema.Properties.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ClaudeSchemaProperty
                    {
                        Type = kvp.Value.Type,
                        Description = kvp.Value.Description,
                        Enum = kvp.Value.Enum,
                        Items = kvp.Value.Items != null ? new ClaudeSchemaProperty
                        {
                            Type = kvp.Value.Items.Type,
                            Description = kvp.Value.Items.Description
                        } : null
                    }),
                Required = mcpTool.InputSchema.Required
            }
        }).ToList();
    }
}