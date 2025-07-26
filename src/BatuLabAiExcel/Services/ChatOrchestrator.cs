using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Orchestrates the chat flow between AI providers and MCP
/// </summary>
public class ChatOrchestrator : IChatOrchestrator
{
    private readonly IAiServiceFactory _aiServiceFactory;
    private readonly IMcpClient _mcpClient;
    private readonly ILogger<ChatOrchestrator> _logger;
    private readonly AppConfiguration.AiProviderSettings _providerSettings;

    private readonly List<AiMessage> _conversationHistory = new();
    private string _currentProvider = "Claude";

    public ChatOrchestrator(
        IAiServiceFactory aiServiceFactory,
        IMcpClient mcpClient,
        IOptions<AppConfiguration.AiProviderSettings> providerSettings,
        ILogger<ChatOrchestrator> logger)
    {
        _aiServiceFactory = aiServiceFactory;
        _mcpClient = mcpClient;
        _providerSettings = providerSettings.Value;
        _logger = logger;
        _currentProvider = _providerSettings.DefaultProvider;
    }

    public async Task<Result<string>> ProcessMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var aiService = _aiServiceFactory.GetAiService(_currentProvider);
            _logger.LogInformation("Starting message processing with {Provider}: {Message}", aiService.ProviderName, message);

            // Ensure MCP client is initialized
            var initResult = await _mcpClient.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
            if (!initResult.IsSuccess)
            {
                _logger.LogError("MCP client initialization failed: {Error}", initResult.Error);
                return Result<string>.Failure($"Excel integration not available: {initResult.Error}");
            }

            // Get available tools from MCP
            var toolsResult = await _mcpClient.GetAvailableToolsAsync(cancellationToken).ConfigureAwait(false);
            if (!toolsResult.IsSuccess)
            {
                _logger.LogWarning("Could not retrieve MCP tools: {Error}", toolsResult.Error);
                // Continue without tools, Claude might still be able to help
            }

            // Add user message to conversation history
            _conversationHistory.Add(new AiMessage
            {
                Role = "user",
                Content = new List<AiMessageContent>
                {
                    new AiMessageContent
                    {
                        Type = "text",
                        Text = message
                    }
                }
            });

            // Prepare AI request with tools
            var aiTools = toolsResult.IsSuccess ? ConvertMcpToolsToAiTools(toolsResult.Value!) : null;

            var maxRounds = 10; // Prevent infinite loops
            var currentRound = 0;

            while (currentRound < maxRounds)
            {
                currentRound++;
                _logger.LogDebug("Processing round {Round}/{MaxRounds}", currentRound, maxRounds);

                // Send request to AI service
                var aiResult = await aiService.SendMessageAsync(
                    _conversationHistory.ToList(), // Make a copy
                    aiTools,
                    cancellationToken).ConfigureAwait(false);

                if (!aiResult.IsSuccess)
                {
                    _logger.LogError("{Provider} request failed: {Error}", aiService.ProviderName, aiResult.Error);
                    return Result<string>.Failure($"AI service error: {aiResult.Error}");
                }

                var response = aiResult.Value!;
                
                // Add AI response to conversation history
                var cleanContent = response.Content.Select(block => new AiMessageContent
                {
                    Type = block.Type,
                    Text = block.Text,
                    ToolUseId = block.ToolUseId,
                    ToolName = block.ToolName,
                    ToolInput = block.ToolInput
                }).ToList();

                _conversationHistory.Add(new AiMessage
                {
                    Role = "assistant",
                    Content = cleanContent
                });

                // Check if AI wants to use tools
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

                    _logger.LogInformation("Completed processing with {Provider} in {Rounds} rounds", aiService.ProviderName, currentRound);
                    return Result<string>.Success(textContent ?? "No response from AI.");
                }

                // Process tool uses
                var toolResults = new List<AiMessageContent>();

                foreach (var toolUse in toolUses)
                {
                    // Check for cancellation before each tool call
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (string.IsNullOrEmpty(toolUse.ToolUseId) || string.IsNullOrEmpty(toolUse.ToolName))
                    {
                        _logger.LogWarning("Invalid tool use: missing ID or name");
                        continue;
                    }

                    _logger.LogInformation("Executing tool: {ToolName} with ID: {ToolId}", toolUse.ToolName, toolUse.ToolUseId);

                    try
                    {
                        // Call MCP tool
                        var mcpResult = await _mcpClient.CallToolAsync(
                            toolUse.ToolName!,
                            toolUse.ToolInput,
                            cancellationToken).ConfigureAwait(false);

                        var toolResult = new AiMessageContent
                        {
                            Type = "tool_result",
                            ToolUseId = toolUse.ToolUseId,
                            ToolResult = mcpResult.IsSuccess 
                                ? (mcpResult.Value ?? "Tool executed successfully")
                                : $"Error: {mcpResult.Error}",
                            IsError = !mcpResult.IsSuccess
                        };

                        toolResults.Add(toolResult);

                        _logger.LogInformation("Tool {ToolName} executed with result: {Success}", 
                            toolUse.ToolName, mcpResult.IsSuccess ? "Success" : "Error");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception executing tool {ToolName}", toolUse.ToolName);
                        
                        toolResults.Add(new AiMessageContent
                        {
                            Type = "tool_result",
                            ToolUseId = toolUse.ToolUseId,
                            ToolResult = $"Tool execution failed: {ex.Message}",
                            IsError = true
                        });
                    }
                }

                // Add tool results to conversation history
                if (toolResults.Any())
                {
                    _conversationHistory.Add(new AiMessage
                    {
                        Role = "user",
                        Content = toolResults
                    });
                }

                // Continue the loop to get AI's response to the tool results
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

    private List<AiTool> ConvertMcpToolsToAiTools(List<McpTool> mcpTools)
    {
        var aiTools = new List<AiTool>();
        
        foreach (var mcpTool in mcpTools)
        {
            try
            {
                var aiTool = new AiTool
                {
                    Name = mcpTool.Name,
                    Description = mcpTool.Description,
                    Parameters = ConvertToAiToolSchema(mcpTool.InputSchema)
                };
                
                aiTools.Add(aiTool);
                _logger.LogDebug("Successfully converted MCP tool: {ToolName}", mcpTool.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert MCP tool {ToolName}, skipping", mcpTool.Name);
                // Continue with other tools rather than failing completely
            }
        }
        
        _logger.LogInformation("Converted {Count}/{Total} MCP tools to AI format", 
            aiTools.Count, mcpTools.Count);
        
        return aiTools;
    }

    private AiToolSchema ConvertToAiToolSchema(McpToolSchema mcpSchema)
    {
        var schema = new AiToolSchema
        {
            Type = "object" // Always object for tool schemas
        };

        // Only set properties if they exist and are valid
        if (mcpSchema.Properties?.Any() == true)
        {
            var validProperties = new Dictionary<string, AiToolProperty>();
            
            foreach (var prop in mcpSchema.Properties)
            {
                if (!string.IsNullOrWhiteSpace(prop.Key))
                {
                    try
                    {
                        var convertedProp = ConvertMcpPropertyToAiProperty(prop.Value);
                        validProperties[prop.Key] = convertedProp;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Skipping invalid property {PropertyName}", prop.Key);
                    }
                }
            }
            
            if (validProperties.Any())
            {
                schema.Properties = validProperties;
            }
        }

        // Only set required if there are actual required fields
        if (mcpSchema.Required?.Any() == true)
        {
            var validRequired = mcpSchema.Required
                .Where(r => !string.IsNullOrWhiteSpace(r) && 
                           schema.Properties?.ContainsKey(r) == true)
                .ToList();
                
            if (validRequired.Any())
            {
                schema.Required = validRequired;
            }
        }

        return schema;
    }

    private AiToolProperty ConvertMcpPropertyToAiProperty(McpSchemaProperty mcpProperty)
    {
        var property = new AiToolProperty();
        
        // Strict type validation - only allow supported types
        var validTypes = new[] { "string", "number", "integer", "boolean", "array", "object" };
        var mcpType = mcpProperty.Type?.ToLowerInvariant();
        
        if (!string.IsNullOrEmpty(mcpType) && validTypes.Contains(mcpType))
        {
            property.Type = mcpType;
        }
        else
        {
            // Default to string for unknown types
            property.Type = "string";
            _logger.LogDebug("Unknown type {Type} converted to string", mcpProperty.Type);
        }

        // Clean description - keep it simple
        if (!string.IsNullOrWhiteSpace(mcpProperty.Description))
        {
            var description = mcpProperty.Description.Trim();
            // Limit description length to avoid schema bloat
            if (description.Length > 200)
            {
                description = description[..197] + "...";
            }
            property.Description = description;
        }

        // Handle enum values with strict validation
        if (mcpProperty.Enum?.Any() == true)
        {
            var validEnums = mcpProperty.Enum
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct()
                .ToList();
                
            if (validEnums.Any())
            {
                property.Enum = validEnums;
            }
        }

        // Handle array items with validation
        if (property.Type == "array" && mcpProperty.Items != null)
        {
            try
            {
                property.Items = ConvertMcpPropertyToAiProperty(mcpProperty.Items);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert array items, using string default");
                property.Items = new AiToolProperty { Type = "string" };
            }
        }

        return property;
    }

    public void SetCurrentProvider(string provider)
    {
        if (_aiServiceFactory.IsProviderAvailable(provider))
        {
            _currentProvider = provider;
            _logger.LogInformation("AI provider changed to {Provider}", provider);
        }
        else
        {
            _logger.LogWarning("Invalid AI provider {Provider}, keeping current provider {Current}", provider, _currentProvider);
        }
    }

    public string GetCurrentProvider() => _currentProvider;
}