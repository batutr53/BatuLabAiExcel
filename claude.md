# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Office Ai - Batu Lab.** is a Windows WPF desktop application (.NET 9) that bridges Claude AI with Excel through the Model Context Protocol (MCP). It features:

- Natural language Excel automation using Claude API
- MCP integration with excel-mcp-server for Excel operations
- Enterprise-grade authentication, licensing, and payment system via Stripe
- Modern MVVM architecture with dependency injection

## Architecture

### Layered Architecture
```
┌─────────────────────┐
│   Presentation      │  ← WPF Views/ViewModels (MVVM)
├─────────────────────┤
│   Service Layer     │  ← AI Services, MCP Client, Chat Orchestrator
├─────────────────────┤
│   Business Layer    │  ← Authentication, License, Payment Services
├─────────────────────┤
│   Data Layer        │  ← Entity Framework Core + PostgreSQL
├─────────────────────┤
│   Infrastructure    │  ← ProcessHelper, HttpRetryHandler, SecureStorage
└─────────────────────┘
```

### Key Services
- **ChatOrchestrator**: Manages AI conversation flow and tool execution
- **ClaudeService**: HTTP client for Claude Messages API with retry policies
- **McpClient**: JSON-RPC client for MCP server communication via stdio
- **AuthenticationService**: JWT-based user authentication
- **LicenseService**: License validation and management
- **PaymentService**: Stripe payment integration

### AI Provider System
The application supports multiple AI providers through a factory pattern:
- **Claude** (primary): Via direct API and desktop automation
- **Gemini**: Google's AI model
- **Groq**: Fast inference AI service
- **Claude CLI**: Uses claude-code CLI tool with MCP

## Common Development Commands

### Building and Running
```bash
# Build the solution
dotnet build

# Run the application
dotnet run --project src\BatuLabAiExcel

# Run with specific environment
dotnet run --project src\BatuLabAiExcel --environment Development

# Build for release
dotnet build --configuration Release

# Publish for deployment
dotnet publish src\BatuLabAiExcel -c Release -r win-x64 --self-contained true
```

### Database Operations
```bash
# Add migration
dotnet ef migrations add MigrationName --project src\BatuLabAiExcel

# Update database
dotnet ef database update --project src\BatuLabAiExcel

# Drop database (development only)
dotnet ef database drop -f --project src\BatuLabAiExcel
```

### Setup Scripts
```powershell
# Complete environment setup
.\scripts\setup_everything.ps1

# Setup MCP backend only
.\scripts\setup_mcp.ps1

# Setup database
.\scripts\setup_database.ps1

# Verify backend health
.\scripts\run_backend_check.ps1

# Install Python dependencies
.\scripts\setup_python_and_mcp.ps1
```

### Testing and Debugging
```powershell
# Run comprehensive test scenarios
.\scripts\test_scenarios.ps1

# Diagnose Python installation
.\scripts\diagnose_python.ps1

# Quick fix for common issues
.\scripts\quick_fix.ps1
```

## Key Configuration

### appsettings.json Structure
- **Claude**: API key, model settings, retry policies
- **Gemini**: Google AI configuration
- **Groq**: Groq API settings
- **Mcp**: Python path, server script, working directory
- **Database**: PostgreSQL connection string
- **Authentication**: JWT settings, password policies
- **Stripe**: Payment configuration, webhook secrets
- **License**: Trial duration, pricing, validation settings

### Environment Variables
- `CLAUDE_API_KEY`: Override Claude API key
- `ASPNETCORE_ENVIRONMENT`: Set to Development for debug mode
- `MCP_WORKING_DIRECTORY`: Override Excel files directory

## MCP Integration

The application communicates with excel-mcp-server through JSON-RPC over stdio. Key tools include:

### Workbook Operations
- `create_workbook`: Create new Excel files
- `get_workbook_metadata`: Get workbook information
- `create_worksheet`: Add new worksheets

### Data Operations  
- `read_data_from_excel`: Read cell ranges
- `write_data_to_excel`: Write data to cells

### Advanced Operations
- `format_range`: Apply cell formatting
- `apply_formula`: Add Excel formulas
- `create_chart`: Generate charts
- `create_pivot_table`: Create pivot tables

## Authentication & Licensing

### User Flow
1. **Registration**: Creates account with 1-day trial license
2. **Trial Validation**: Checks license status on startup
3. **Subscription**: Stripe integration for monthly/yearly/lifetime plans
4. **License Validation**: Local + remote validation with grace period

### Security Features
- BCrypt password hashing
- JWT token authentication
- Windows Credential Manager for secure storage
- API key masking in logs
- Rate limiting and retry policies

## Development Guidelines

### Code Standards
- Use XML documentation for public APIs
- Follow MVVM pattern strictly
- Implement Result pattern for error handling
- Use CancellationToken for all async operations
- Apply dependency injection throughout

### Error Handling
- All services return Result<T> types
- Comprehensive logging with Serilog
- User-friendly error messages in UI
- Graceful degradation for non-critical failures

### UI/UX Conventions
- Dark theme with golden ratio proportions
- Card-based layout for modern appearance
- Loading states for async operations
- Clear visual feedback for user actions

## Troubleshooting

### Common Issues
1. **MCP server not starting**: Check Python installation and excel-mcp-server package
2. **Database connection failed**: Verify PostgreSQL service and connection string
3. **API key errors**: Check Claude API key format and validity
4. **License validation issues**: Review database connectivity and Stripe configuration

### Log Locations
- Application logs: `logs/office-ai-batu-lab-{date}.log`
- MCP communication: Debug level logging
- Authentication events: Information level logging

### Debug Mode
Set `ASPNETCORE_ENVIRONMENT=Development` for:
- Detailed logging output
- Additional error information
- Database auto-migration
- Development-specific configuration
