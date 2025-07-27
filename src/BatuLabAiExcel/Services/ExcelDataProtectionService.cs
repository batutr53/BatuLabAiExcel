using Microsoft.Extensions.Logging;
using System.Text.Json;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Service to protect Excel data and ensure AI operations preserve existing data
/// </summary>
public class ExcelDataProtectionService : IExcelDataProtectionService
{
    private readonly IMcpClient _mcpClient;
    private readonly ILogger<ExcelDataProtectionService> _logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ExcelDataProtectionService(IMcpClient mcpClient, ILogger<ExcelDataProtectionService> logger)
    {
        _mcpClient = mcpClient;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes current Excel content before making any modifications
    /// </summary>
    public async Task<Result<ExcelAnalysisResult>> AnalyzeCurrentContentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Excel content analysis for data protection");

            // Get workbook info
            var workbookInfo = await GetWorkbookInfoAsync(cancellationToken);
            if (!workbookInfo.IsSuccess)
            {
                return Result<ExcelAnalysisResult>.Failure($"Could not analyze workbook: {workbookInfo.Error}");
            }

            var analysis = new ExcelAnalysisResult
            {
                WorkbookInfo = workbookInfo.Value!,
                Sheets = new List<SheetAnalysis>()
            };

            // Analyze each sheet
            foreach (var sheetName in workbookInfo.Value!.SheetNames)
            {
                var sheetAnalysis = await AnalyzeSheetAsync(sheetName, cancellationToken);
                if (sheetAnalysis.IsSuccess)
                {
                    analysis.Sheets.Add(sheetAnalysis.Value!);
                }
            }

            _logger.LogInformation("Excel analysis completed. Found {SheetCount} sheets with data", analysis.Sheets.Count);
            return Result<ExcelAnalysisResult>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing Excel content");
            return Result<ExcelAnalysisResult>.Failure($"Analysis failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a safe operation plan that preserves existing data
    /// </summary>
    public async Task<Result<SafeOperationPlan>> CreateSafeOperationPlanAsync(
        string userRequest,
        ExcelAnalysisResult currentContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating safe operation plan for request: {Request}", userRequest);

            var plan = new SafeOperationPlan
            {
                UserRequest = userRequest,
                PreservationStrategy = DeterminePreservationStrategy(userRequest, currentContent),
                Steps = new List<SafeOperationStep>()
            };

            // Determine what data needs to be preserved
            plan.DataToPreserve = IdentifyDataToPreserve(currentContent, userRequest);

            // Create backup strategy if needed
            if (RequiresBackup(userRequest))
            {
                plan.BackupRequired = true;
                plan.Steps.Add(new SafeOperationStep
                {
                    Type = OperationStepType.Backup,
                    Description = "Create backup of current data",
                    TargetRange = "All data",
                    Order = 1
                });
            }

            // Add data preservation steps
            var preservationSteps = CreateDataPreservationSteps(plan.DataToPreserve, userRequest);
            plan.Steps.AddRange(preservationSteps);

            _logger.LogInformation("Safe operation plan created with {StepCount} steps", plan.Steps.Count);
            return Result<SafeOperationPlan>.Success(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating safe operation plan");
            return Result<SafeOperationPlan>.Failure($"Plan creation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates an operation to ensure it doesn't destroy existing data
    /// </summary>
    public Result ValidateOperation(string toolName, object parameters, ExcelAnalysisResult currentContent)
    {
        try
        {
            _logger.LogDebug("Validating operation: {ToolName}", toolName);

            // Check for destructive operations
            var destructiveOperations = new[] 
            { 
                "excel/clear_sheet", 
                "excel/delete_sheet", 
                "excel/clear_range",
                "excel/delete_rows",
                "excel/delete_columns"
            };

            if (destructiveOperations.Contains(toolName))
            {
                _logger.LogWarning("Potentially destructive operation detected: {ToolName}", toolName);
                return Result.Failure($"Operation '{toolName}' could destroy existing data. Please specify preservation requirements.");
            }

            // Check write operations that might overwrite data
            if (toolName.Contains("write") || toolName.Contains("set"))
            {
                return ValidateWriteOperation(toolName, parameters, currentContent);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating operation {ToolName}", toolName);
            return Result.Failure($"Validation failed: {ex.Message}");
        }
    }

    private async Task<Result<WorkbookInfo>> GetWorkbookInfoAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Try to get sheet names first (most basic operation)
            var sheetsResult = await _mcpClient.CallToolAsync("get_sheet_names", new { }, cancellationToken);
            if (!sheetsResult.IsSuccess)
            {
                return Result<WorkbookInfo>.Failure($"Could not get sheet names: {sheetsResult.Error}");
            }

            var workbookInfo = new WorkbookInfo();
            
            // Parse sheet names from the result
            if (!string.IsNullOrEmpty(sheetsResult.Value))
            {
                try
                {
                    // Assuming the result is a JSON array of sheet names
                    var sheetNames = JsonSerializer.Deserialize<List<string>>(sheetsResult.Value, JsonOptions);
                    if (sheetNames != null)
                    {
                        workbookInfo.SheetNames = sheetNames;
                        workbookInfo.TotalSheets = sheetNames.Count;
                    }
                }
                catch
                {
                    // If not JSON, try to parse as simple text
                    var lines = sheetsResult.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    workbookInfo.SheetNames = lines.ToList();
                    workbookInfo.TotalSheets = lines.Length;
                }
            }

            return Result<WorkbookInfo>.Success(workbookInfo);
        }
        catch (Exception ex)
        {
            return Result<WorkbookInfo>.Failure($"Failed to get workbook info: {ex.Message}");
        }
    }

    private async Task<Result<SheetAnalysis>> AnalyzeSheetAsync(string sheetName, CancellationToken cancellationToken)
    {
        try
        {
            // Try to get basic sheet info first
            var analysis = new SheetAnalysis
            {
                SheetName = sheetName,
                HasData = false,
                UsedRange = "A1",
                RowCount = 0,
                ColumnCount = 0
            };

            // Try to read a small range to check if there's data
            var testResult = await _mcpClient.CallToolAsync("read_range", 
                new { range = "A1:J20", sheet_name = sheetName }, cancellationToken);

            if (testResult.IsSuccess && !string.IsNullOrEmpty(testResult.Value))
            {
                analysis.HasData = true;
                analysis.DataSample = testResult.Value.Length > 500 ? 
                    testResult.Value.Substring(0, 500) + "..." : testResult.Value;
                
                // Try to determine used range by analyzing the data
                var lines = testResult.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                analysis.RowCount = lines.Length;
                
                if (lines.Length > 0)
                {
                    // Estimate column count from first line
                    var firstLineColumns = lines[0].Split('\t', ',').Length;
                    analysis.ColumnCount = firstLineColumns;
                    analysis.UsedRange = $"A1:{GetColumnLetter(firstLineColumns)}{lines.Length}";
                }
            }

            return Result<SheetAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            return Result<SheetAnalysis>.Failure($"Failed to analyze sheet {sheetName}: {ex.Message}");
        }
    }

    private PreservationStrategy DeterminePreservationStrategy(string userRequest, ExcelAnalysisResult content)
    {
        var request = userRequest.ToLowerInvariant();

        // If user explicitly wants to replace/clear
        if (request.Contains("sil") || request.Contains("temizle") || request.Contains("clear") || request.Contains("replace all"))
        {
            return PreservationStrategy.UserRequestedClear;
        }

        // If user wants to add/append
        if (request.Contains("ekle") || request.Contains("add") || request.Contains("append"))
        {
            return PreservationStrategy.PreserveAndAppend;
        }

        // If user wants to modify/update specific data
        if (request.Contains("güncelle") || request.Contains("değiştir") || request.Contains("update") || 
            request.Contains("change") || request.Contains("modify") || request.Contains("format"))
        {
            return PreservationStrategy.PreserveAndModify;
        }

        // Default: preserve everything unless explicitly told otherwise
        return PreservationStrategy.PreserveAll;
    }

    private List<DataPreservationInfo> IdentifyDataToPreserve(ExcelAnalysisResult content, string userRequest)
    {
        var dataToPreserve = new List<DataPreservationInfo>();

        foreach (var sheet in content.Sheets)
        {
            if (sheet.HasData)
            {
                dataToPreserve.Add(new DataPreservationInfo
                {
                    SheetName = sheet.SheetName,
                    Range = sheet.UsedRange,
                    DataType = "existing_data",
                    Priority = PreservationPriority.High,
                    Reason = "Existing user data that should be preserved"
                });
            }
        }

        return dataToPreserve;
    }

    private bool RequiresBackup(string userRequest)
    {
        var request = userRequest.ToLowerInvariant();
        
        // Require backup for potentially destructive operations
        return request.Contains("sil") || request.Contains("temizle") || request.Contains("clear") ||
               request.Contains("replace") || request.Contains("delete") || request.Contains("remove");
    }

    private List<SafeOperationStep> CreateDataPreservationSteps(List<DataPreservationInfo> dataToPreserve, string userRequest)
    {
        var steps = new List<SafeOperationStep>();
        int order = 2; // Start after backup

        foreach (var data in dataToPreserve.Where(d => d.Priority == PreservationPriority.High))
        {
            steps.Add(new SafeOperationStep
            {
                Type = OperationStepType.ReadData,
                Description = $"Read existing data from {data.SheetName}!{data.Range}",
                TargetRange = $"{data.SheetName}!{data.Range}",
                Order = order++
            });
        }

        return steps;
    }

    private Result ValidateWriteOperation(string toolName, object parameters, ExcelAnalysisResult currentContent)
    {
        // Extract range from parameters if possible
        var paramJson = JsonSerializer.Serialize(parameters);
        var paramDict = JsonSerializer.Deserialize<Dictionary<string, object>>(paramJson);

        if (paramDict?.ContainsKey("range") == true)
        {
            var range = paramDict["range"]?.ToString();
            var sheetName = paramDict.ContainsKey("sheet_name") ? paramDict["sheet_name"]?.ToString() : null;

            // Check if this range contains existing data
            var targetSheet = currentContent.Sheets.FirstOrDefault(s => 
                string.IsNullOrEmpty(sheetName) || s.SheetName.Equals(sheetName, StringComparison.OrdinalIgnoreCase));

            if (targetSheet?.HasData == true && RangeOverlaps(range, targetSheet.UsedRange))
            {
                _logger.LogWarning("Write operation would overwrite existing data in range {Range}", range);
                return Result.Failure($"Operation would overwrite existing data in {sheetName ?? "current sheet"}!{range}. " +
                                     "Please specify how to handle existing data or use a different range.");
            }
        }

        return Result.Success();
    }

    private bool RangeOverlaps(string? range1, string range2)
    {
        // Simple overlap check - in a real implementation, this would be more sophisticated
        return !string.IsNullOrEmpty(range1) && !string.IsNullOrEmpty(range2);
    }

    private int ExtractRowCount(string range)
    {
        // Simple implementation - extract row count from range like "A1:C10"
        if (string.IsNullOrEmpty(range) || !range.Contains(':'))
            return 1;

        var parts = range.Split(':');
        if (parts.Length != 2) return 1;

        var endCell = parts[1];
        var rowPart = new string(endCell.Where(char.IsDigit).ToArray());
        
        return int.TryParse(rowPart, out int row) ? row : 1;
    }

    private int ExtractColumnCount(string range)
    {
        // Simple implementation - extract column count from range like "A1:C10"
        if (string.IsNullOrEmpty(range) || !range.Contains(':'))
            return 1;

        var parts = range.Split(':');
        if (parts.Length != 2) return 1;

        var startCol = new string(parts[0].Where(char.IsLetter).ToArray());
        var endCol = new string(parts[1].Where(char.IsLetter).ToArray());

        return ColumnToNumber(endCol) - ColumnToNumber(startCol) + 1;
    }

    private int ColumnToNumber(string column)
    {
        int result = 0;
        for (int i = 0; i < column.Length; i++)
        {
            result = result * 26 + (column[i] - 'A' + 1);
        }
        return result;
    }

    private string GetColumnLetter(int columnNumber)
    {
        string result = "";
        while (columnNumber > 0)
        {
            columnNumber--;
            result = (char)('A' + columnNumber % 26) + result;
            columnNumber /= 26;
        }
        return result;
    }
}