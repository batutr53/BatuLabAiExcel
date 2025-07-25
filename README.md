# Office Ai - Batu Lab.

**AI-powered Excel automation with Claude integration via MCP (Model Context Protocol)**

![.NET 9](https://img.shields.io/badge/.NET-9.0-blue.svg)
![WPF](https://img.shields.io/badge/UI-WPF-lightblue.svg)
![Claude](https://img.shields.io/badge/AI-Claude-orange.svg)
![MCP](https://img.shields.io/badge/Protocol-MCP-green.svg)

## 🎯 Overview

Office Ai - Batu Lab. is a Windows desktop application that bridges Claude AI with Excel through the Model Context Protocol (MCP). Users can interact with Excel files using natural language, while Claude handles the complex Excel operations through the excel-mcp-server.

### Key Features

- 🤖 **Natural Language Excel Interaction**: Ask Claude to perform Excel operations in plain English
- 📊 **Full Excel Automation**: Read, write, format, create charts, pivot tables, and more
- 🔗 **MCP Integration**: Seamless connection between Claude and Excel via standardized protocol
- 🛡️ **Enterprise-Ready**: Comprehensive logging, error handling, and retry mechanisms
- ⚡ **Modern Architecture**: .NET 9, MVVM, Dependency Injection, and reactive UI

## 🏗️ Architecture

The application follows a clean architecture pattern with the following layers:

### **Presentation Layer**
- **WPF + MVVM**: Modern UI with data binding and commands
- **CommunityToolkit.Mvvm**: Source generators for ViewModels

### **Application Layer**
- **ChatOrchestrator**: Manages the conversation flow between Claude and MCP
- **Generic Host**: Microsoft.Extensions.Hosting for DI and configuration

### **Domain Layer**
- **Models**: DTOs for Claude API, MCP protocol, and UI state
- **Result Pattern**: Type-safe error handling

### **Infrastructure Layer**
- **ClaudeService**: HTTP client for Claude Messages API with retry policies
- **McpClient**: JSON-RPC client for MCP server communication over stdio
- **ProcessHelper**: Safe process management for MCP server lifecycle

## 🚀 Quick Start

### Prerequisites

- **Windows 10/11** (64-bit)
- **.NET 9 Runtime** or SDK
- **Python 3.8+** with pip
- **Claude API Key** from Anthropic
- **Visual Studio 2022** or **VS Code** (for development)

### Installation

1. **Clone and build the project:**
   ```bash
   git clone <repository-url>
   cd batulabaiexcel
   dotnet build
   ```

2. **Set up the MCP backend:**
   ```powershell
   .\scripts\setup_mcp.ps1
   ```

3. **Configure your Claude API key:**
   Edit `src\BatuLabAiExcel\appsettings.json`:
   ```json
   {
     "Claude": {
       "ApiKey": "sk-ant-api03-your-api-key-here"
     }
   }
   ```

4. **Run the application:**
   ```bash
   dotnet run --project src\BatuLabAiExcel
   ```

### Quick Test

1. **Verify backend health:**
   ```powershell
   .\scripts\run_backend_check.ps1
   ```

2. **Test with a sample prompt:**
   > "Create a new Excel file called 'test.xlsx' and add some sample data in Sheet1"

## 📖 Detailed Setup Guide

### 1. Environment Setup

#### Install .NET 9
```bash
# Windows (via winget)
winget install Microsoft.DotNet.SDK.9

# Or download from: https://dotnet.microsoft.com/download/dotnet/9.0
```

#### Install Python Dependencies
The MCP server requires Python with specific packages:

```bash
# Option 1: Using uvx (recommended)
pip install uv
uvx excel-mcp-server stdio --help

# Option 2: Traditional pip
pip install excel-mcp-server
```

### 2. Configuration

#### appsettings.json Structure
```json
{
  "Claude": {
    "ApiKey": "sk-ant-api03-...",
    "Model": "claude-3-5-sonnet-20241022",
    "BaseUrl": "https://api.anthropic.com",
    "MaxTokens": 4096,
    "TimeoutSeconds": 60,
    "RetryCount": 3,
    "RetryDelaySeconds": 2
  },
  "Mcp": {
    "PythonPath": "python",
    "ServerScript": "uvx excel-mcp-server stdio",
    "WorkingDirectory": "./excel_files",
    "TimeoutSeconds": 30,
    "RestartOnFailure": true,
    "MaxRestartAttempts": 3
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "BatuLabAiExcel": "Debug"
    },
    "File": {
      "Path": "logs/office-ai-batu-lab-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 7,
      "FileSizeLimitBytes": 10485760
    }
  }
}
```

#### Environment Variables (Optional)
```bash
# Override API key for security
CLAUDE_API_KEY=sk-ant-api03-...

# Override MCP working directory
MCP_WORKING_DIRECTORY=C:\MyExcelFiles
```

### 3. Development Setup

#### Using Visual Studio 2022
1. Open `BatuLabAiExcel.sln`
2. Set `BatuLabAiExcel` as startup project
3. Press F5 to run

#### Using Visual Studio Code
1. Install C# extension
2. Open project folder
3. Use `dotnet run` in terminal

#### Using CLI Only
```bash
# Build
dotnet build

# Run
dotnet run --project src\BatuLabAiExcel

# Run with specific environment
dotnet run --project src\BatuLabAiExcel --environment Development
```

## 💻 Usage Examples

### Basic Excel Operations

#### Reading Data
```
Prompt: "Read the data from Sheet1 range A1:C10 and summarize what you find."

Expected Flow:
1. Claude receives the prompt
2. Claude calls `read_data_from_excel` tool via MCP
3. MCP server reads Excel file
4. Claude receives the data and provides summary
5. User sees the final summary
```

#### Writing Data
```
Prompt: "Create a budget spreadsheet with categories like Food, Transport, Entertainment in column A, and amounts 500, 200, 150 in column B."

Expected Flow:
1. Claude plans the spreadsheet structure
2. Claude calls `create_workbook` tool (if needed)
3. Claude calls `write_data_to_excel` with structured data
4. User sees confirmation of created spreadsheet
```

#### Data Analysis
```
Prompt: "Analyze the sales data in Sheet1 and create a chart showing monthly trends."

Expected Flow:
1. Claude reads the sales data
2. Claude processes and analyzes the data
3. Claude creates a chart using `create_chart` tool
4. User gets analysis summary with visual chart
```

### Advanced Scenarios

#### Pivot Tables
```
Prompt: "Create a pivot table from the data in A1:D100, group by Region, sum the Sales column."
```

#### Formula Application
```
Prompt: "Add a formula in column E to calculate the percentage of each row relative to the total."
```

#### Formatting
```
Prompt: "Format the header row with bold text and blue background, make the numbers in column C currency format."
```

## 🔧 Troubleshooting

### Common Issues

#### 1. "Claude API key is not configured"
**Solution:**
- Ensure `appsettings.json` has valid API key
- Check environment variable `CLAUDE_API_KEY`
- Verify API key format: `sk-ant-api03-...`

#### 2. "Excel integration not available"
**Solution:**
```powershell
# Re-run MCP setup
.\scripts\setup_mcp.ps1 -Force

# Check backend health
.\scripts\run_backend_check.ps1 -Verbose
```

#### 3. "MCP server process exited immediately"
**Solutions:**
- Check Python installation: `python --version`
- Verify excel-mcp-server: `uvx excel-mcp-server stdio --help`
- Check working directory permissions
- Review logs in `logs/office-ai-batu-lab-.log`

#### 4. "Request timed out"
**Solutions:**
- Increase timeout in `appsettings.json`
- Check network connection
- Try simpler prompts first
- Verify Claude API status

### Debug Mode

Enable detailed logging by setting environment:
```bash
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src\BatuLabAiExcel
```

### Log Analysis

Logs are stored in `logs/office-ai-batu-lab-.log`:
```bash
# View recent logs
Get-Content logs\office-ai-batu-lab-*.log -Tail 50

# Search for errors
Select-String "ERROR" logs\office-ai-batu-lab-*.log
```

## 🧪 Testing

### Unit Tests
*TODO: Add unit test project*

### Integration Testing

#### Manual Integration Test
1. Start the application
2. Enter test prompt: "Create a test Excel file with sample data"
3. Verify file creation in working directory
4. Check application logs for complete flow

#### Automated Backend Test
```powershell
.\scripts\run_backend_check.ps1 -Verbose
```

### Performance Testing

#### Load Testing Scenarios
- Multiple consecutive requests
- Large Excel file operations
- Complex pivot table creation
- Chart generation with large datasets

#### Memory and Resource Monitoring
- Process memory usage during large operations
- MCP server resource consumption
- File handle management

## 🚢 Deployment

### Development Deployment
```bash
# Build for development
dotnet build --configuration Debug

# Run with development settings
dotnet run --project src\BatuLabAiExcel --environment Development
```

### Production Deployment

#### Self-Contained Deployment
```bash
# Build standalone executable
dotnet publish src\BatuLabAiExcel -c Release -r win-x64 --self-contained true

# Output: src\BatuLabAiExcel\bin\Release\net9.0-windows\win-x64\publish\
```

#### Framework-Dependent Deployment
```bash
# Requires .NET 9 runtime on target machine
dotnet publish src\BatuLabAiExcel -c Release

# Smaller deployment size
```

#### Deployment Package Structure
```
BatuLabAiExcel-v1.0.0\
├── BatuLabAiExcel.exe
├── appsettings.json
├── appsettings.Production.json
├── scripts\
│   ├── setup_mcp.ps1
│   └── run_backend_check.ps1
├── excel_files\
└── README_DEPLOYMENT.txt
```

#### Production Checklist
- [ ] Configure production API keys
- [ ] Set appropriate logging levels
- [ ] Configure working directories
- [ ] Test MCP server installation
- [ ] Verify Python dependencies
- [ ] Test with sample Excel files
- [ ] Configure Windows Defender exclusions
- [ ] Set up backup for Excel files

### Docker Deployment (Advanced)
*Note: Requires Windows containers for WPF*

```dockerfile
# TODO: Windows container support
FROM mcr.microsoft.com/dotnet/runtime:9.0-windowsservercore
# ... container setup
```

## 🔒 Security Considerations

### API Key Management
- **Never commit API keys to source control**
- Use environment variables or secure vaults in production
- Implement API key rotation procedures
- Monitor API usage and costs

### File System Security
- Restrict MCP working directory permissions
- Validate Excel file paths to prevent directory traversal
- Implement file size limits
- Scan uploaded files for malicious content

### Network Security
- Use HTTPS for all Claude API communications
- Implement certificate pinning for production
- Configure firewall rules for outbound connections
- Monitor network traffic for anomalies

### Process Security
- Run MCP server with minimal privileges
- Implement process isolation
- Monitor and limit resource usage
- Handle process crashes gracefully

## 📊 Monitoring and Observability

### Logging Strategy
- **Trace**: Detailed MCP communication
- **Debug**: Internal application flow
- **Information**: User actions and results
- **Warning**: Recoverable errors and retries
- **Error**: Failures requiring attention
- **Critical**: Application-level failures

### Metrics to Monitor
- Claude API response times
- MCP server health and uptime
- Excel operation success rates
- Memory and CPU usage
- Error rates by category

### Health Checks
```powershell
# Automated health monitoring
.\scripts\run_backend_check.ps1 -WorkingDirectory "C:\Production\ExcelFiles"
```

## 🤝 Contributing

### Development Setup
1. Fork the repository
2. Clone your fork
3. Create feature branch: `git checkout -b feature/my-feature`
4. Make changes and test
5. Submit pull request

### Code Standards
- Follow C# coding conventions
- Use XML documentation for public APIs
- Implement proper error handling
- Add unit tests for new features
- Update README for user-facing changes

### Architecture Guidelines
- Maintain separation of concerns
- Use dependency injection
- Implement Result pattern for error handling
- Keep UI logic in ViewModels
- Make services stateless where possible

## 📄 License

*TODO: Add license information*

## 🙋‍♀️ Support

### Documentation
- **README.md**: This comprehensive guide
- **Code Comments**: Inline documentation
- **XML Docs**: API documentation

### Getting Help
1. Check troubleshooting section above
2. Review application logs
3. Run health check script
4. Create GitHub issue with:
   - Application version
   - Error messages
   - Steps to reproduce
   - System configuration

### Known Limitations
- Windows-only (WPF requirement)
- Requires internet connection for Claude API
- Excel files must be accessible to MCP server
- Limited to supported Excel operations by excel-mcp-server

---

## 🔧 Excel MCP Server Tool Reference

Based on the excel-mcp-server repository analysis, the following tools are available:

### Workbook Operations
- `create_workbook(filepath: str)` - Create new Excel workbook
- `create_worksheet(filepath: str, sheet_name: str)` - Add new worksheet
- `get_workbook_metadata(filepath: str, include_ranges: bool = False)` - Get workbook information

### Data Operations
- `write_data_to_excel(filepath: str, sheet_name: str, data: List[Dict], start_cell: str = "A1")` - Write data to cells
- `read_data_from_excel(filepath: str, sheet_name: str, start_cell: str = "A1", end_cell: str = None, preview_only: bool = False)` - Read cell data

### Formatting Operations
- `format_range(filepath: str, sheet_name: str, start_cell: str, end_cell: str = None, ...)` - Apply formatting
- `merge_cells(filepath: str, sheet_name: str, start_cell: str, end_cell: str)` - Merge cell ranges
- `unmerge_cells(filepath: str, sheet_name: str, start_cell: str, end_cell: str)` - Unmerge cells

### Advanced Operations
- `apply_formula(filepath: str, sheet_name: str, cell: str, formula: str)` - Apply Excel formulas
- `create_chart(filepath: str, sheet_name: str, data_range: str, chart_type: str, ...)` - Create charts
- `create_pivot_table(filepath: str, sheet_name: str, data_range: str, ...)` - Create pivot tables

*TODO: Verify exact parameter schemas from repository*

## 📈 Example Payload Structures

### Claude Request with Tools
```json
{
  "model": "claude-3-5-sonnet-20241022",
  "max_tokens": 4096,
  "messages": [
    {
      "role": "user",
      "content": "Read data from Sheet1!A1:C3"
    }
  ],
  "tools": [
    {
      "name": "read_data_from_excel",
      "description": "Read data from Excel cells",
      "input_schema": {
        "type": "object",
        "properties": {
          "filepath": {"type": "string"},
          "sheet_name": {"type": "string"},
          "start_cell": {"type": "string"},
          "end_cell": {"type": "string"}
        },
        "required": ["filepath", "sheet_name"]
      }
    }
  ]
}
```

### MCP Tool Call Request
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "tools/call",
  "params": {
    "name": "read_data_from_excel",
    "arguments": {
      "filepath": "sample.xlsx",
      "sheet_name": "Sheet1",
      "start_cell": "A1",
      "end_cell": "C3"
    }
  }
}
```

### MCP Tool Call Response
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Data read successfully:\nA1: Name, B1: Age, C1: City\nA2: John, B2: 30, C2: New York\nA3: Jane, B3: 25, C3: Boston"
      }
    ],
    "isError": false
  }
}
```

---

**Office Ai - Batu Lab.** - Bridging AI and Excel through modern protocols and architecture.

*Last updated: $(Get-Date -Format "yyyy-MM-dd")*