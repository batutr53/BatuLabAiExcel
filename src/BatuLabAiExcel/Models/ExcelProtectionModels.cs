namespace BatuLabAiExcel.Models;

/// <summary>
/// Result of Excel content analysis
/// </summary>
public class ExcelAnalysisResult
{
    public WorkbookInfo WorkbookInfo { get; set; } = new();
    public List<SheetAnalysis> Sheets { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Information about the Excel workbook
/// </summary>
public class WorkbookInfo
{
    public string? FileName { get; set; }
    public List<string> SheetNames { get; set; } = new();
    public bool IsProtected { get; set; }
    public int TotalSheets { get; set; }
}

/// <summary>
/// Analysis of a single Excel sheet
/// </summary>
public class SheetAnalysis
{
    public string SheetName { get; set; } = string.Empty;
    public string UsedRange { get; set; } = string.Empty;
    public bool HasData { get; set; }
    public string? DataSample { get; set; }
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public bool IsProtected { get; set; }
    public List<string> TableNames { get; set; } = new();
    public List<string> NamedRanges { get; set; } = new();
}

/// <summary>
/// Safe operation plan to preserve existing data
/// </summary>
public class SafeOperationPlan
{
    public string UserRequest { get; set; } = string.Empty;
    public PreservationStrategy PreservationStrategy { get; set; }
    public List<SafeOperationStep> Steps { get; set; } = new();
    public List<DataPreservationInfo> DataToPreserve { get; set; } = new();
    public bool BackupRequired { get; set; }
    public string? BackupLocation { get; set; }
}

/// <summary>
/// A single step in the safe operation plan
/// </summary>
public class SafeOperationStep
{
    public OperationStepType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? TargetRange { get; set; }
    public int Order { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsCompleted { get; set; }
}

/// <summary>
/// Information about data that needs to be preserved
/// </summary>
public class DataPreservationInfo
{
    public string SheetName { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public PreservationPriority Priority { get; set; }
    public string Reason { get; set; } = string.Empty;
    public object? BackupData { get; set; }
}

/// <summary>
/// Strategy for preserving existing data
/// </summary>
public enum PreservationStrategy
{
    /// <summary>
    /// Preserve all existing data
    /// </summary>
    PreserveAll,
    
    /// <summary>
    /// Preserve existing data and append new data
    /// </summary>
    PreserveAndAppend,
    
    /// <summary>
    /// Preserve existing data but allow modifications
    /// </summary>
    PreserveAndModify,
    
    /// <summary>
    /// User explicitly requested to clear/replace data
    /// </summary>
    UserRequestedClear
}

/// <summary>
/// Type of operation step
/// </summary>
public enum OperationStepType
{
    /// <summary>
    /// Create backup of data
    /// </summary>
    Backup,
    
    /// <summary>
    /// Read existing data
    /// </summary>
    ReadData,
    
    /// <summary>
    /// Validate operation safety
    /// </summary>
    Validate,
    
    /// <summary>
    /// Execute the actual operation
    /// </summary>
    Execute,
    
    /// <summary>
    /// Restore data if needed
    /// </summary>
    Restore
}

/// <summary>
/// Priority level for data preservation
/// </summary>
public enum PreservationPriority
{
    /// <summary>
    /// Low priority - can be modified or replaced
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium priority - should be preserved if possible
    /// </summary>
    Medium,
    
    /// <summary>
    /// High priority - must be preserved
    /// </summary>
    High,
    
    /// <summary>
    /// Critical priority - never modify or delete
    /// </summary>
    Critical
}