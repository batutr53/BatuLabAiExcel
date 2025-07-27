using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Interface for Excel data protection service
/// </summary>
public interface IExcelDataProtectionService
{
    /// <summary>
    /// Analyzes current Excel content before making any modifications
    /// </summary>
    Task<Result<ExcelAnalysisResult>> AnalyzeCurrentContentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a safe operation plan that preserves existing data
    /// </summary>
    Task<Result<SafeOperationPlan>> CreateSafeOperationPlanAsync(
        string userRequest,
        ExcelAnalysisResult currentContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an operation to ensure it doesn't destroy existing data
    /// </summary>
    Result ValidateOperation(string toolName, object parameters, ExcelAnalysisResult currentContent);
}