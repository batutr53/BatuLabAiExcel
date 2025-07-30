# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Office Ai - Batu Lab.** is a Windows WPF desktop application (.NET 9) that bridges Claude AI with Excel through the Model Context Protocol (MCP). It features:

- Natural language Excel automation using Claude API
- MCP integration with excel-mcp-server for Excel operations
- Enterprise-grade authentication, licensing, and payment system via Stripe
- Modern MVVM architecture with dependency injection

## Architecture

### Dual Architecture: Desktop + WebApi
```
┌─────────────────────────────────────────────────────────────┐
│                    DESKTOP APPLICATION                      │
├─────────────────────┬───────────────────────────────────────┤
│   Presentation      │  ← WPF Views/ViewModels (MVVM)        │
├─────────────────────┼───────────────────────────────────────┤
│   Service Layer     │  ← AI Services, MCP Client, Chat Orch │
├─────────────────────┼───────────────────────────────────────┤
│   Infrastructure    │  ← ProcessHelper, SecureStorage       │
└─────────────────────┴───────────────────────────────────────┘
                              │ HTTP/WebAPI
┌─────────────────────────────────────────────────────────────┐
│                      WEBAPI BACKEND                        │
├─────────────────────┬───────────────────────────────────────┤
│   Controllers       │  ← Auth, License, Payment, Admin APIs │
├─────────────────────┼───────────────────────────────────────┤
│   Business Services │  ← Authentication, License, Payment   │
├─────────────────────┼───────────────────────────────────────┤
│   Data Layer        │  ← Entity Framework Core + SQLite     │
└─────────────────────┴───────────────────────────────────────┘
                              │ HTTP
┌─────────────────────────────────────────────────────────────┐
│                      ADMIN PANEL                           │
│                   React + TypeScript                       │
│              Vite + TailwindCSS + Axios                    │
└─────────────────────────────────────────────────────────────┘
```

### Key Services

#### Desktop Application Services
- **ChatOrchestrator**: Manages AI conversation flow and tool execution
- **AiServiceFactory**: Factory pattern for multiple AI providers (Claude, Gemini, Groq, Claude CLI)
- **ClaudeService**: HTTP client for Claude Messages API with retry policies
- **McpClient**: JSON-RPC client for MCP server communication via stdio
- **WebApiClient**: HTTP client for backend API communication
- **SecureStorageService**: Windows Credential Manager integration
- **ExcelDataProtectionService**: Excel file security and validation

#### WebApi Backend Services
- **AuthenticationService**: JWT-based user authentication with BCrypt
- **LicenseService**: License validation and management
- **PaymentService**: Stripe payment integration
- **EmailService**: SMTP-based email delivery via MailKit
- **UserManagementService**: User CRUD operations and administration
- **NotificationService**: User notification system

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
# Add migration (WebApi project handles EF migrations)
dotnet ef migrations add MigrationName --project src\BatuLabAiExcel.WebApi

# Update database
dotnet ef database update --project src\BatuLabAiExcel.WebApi

# Drop database (development only)
dotnet ef database drop -f --project src\BatuLabAiExcel.WebApi
```

### Setup Scripts
```powershell
# Setup MCP backend only
.\scripts\setup_mcp.ps1

# Setup database
.\scripts\setup_database.ps1

# Verify backend health
.\scripts\run_backend_check.ps1

# Diagnose Python installation
.\scripts\diagnose_python.ps1

# Clean and rebuild solution
.\scripts\clean_build.ps1

# Add Excel file to working directory
.\scripts\add_excel_file.ps1

# Deploy WebApi to production
.\scripts\deploy_webapi.ps1
```

### Testing and Debugging
```powershell
# Run comprehensive test scenarios
.\scripts\test_scenarios.ps1

# Test Stripe webhook integration
.\scripts\test_stripe_webhook.ps1
```

### Solution Development
```bash
# Build entire solution
dotnet build BatuLabAiExcel.sln

# Clean and rebuild solution  
dotnet clean BatuLabAiExcel.sln && dotnet build BatuLabAiExcel.sln

# Run tests (if available)
dotnet test BatuLabAiExcel.sln
```

### WebApi Development
```bash
# Run the WebApi project
dotnet run --project src\BatuLabAiExcel.WebApi

# Run WebApi in Development mode
dotnet run --project src\BatuLabAiExcel.WebApi --environment Development

# Build WebApi for production
dotnet build src\BatuLabAiExcel.WebApi --configuration Release
```

### Admin Panel Development
```bash
# Navigate to admin panel directory
cd admin-panel

# Install dependencies
npm install

# Run development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run linting
npm run lint
```

## Key Configuration

### appsettings.json Structure

#### Desktop Application (src\BatuLabAiExcel\appsettings.json)
- **Claude**: API key, model settings, retry policies
- **Gemini**: Google AI configuration  
- **Groq**: Groq API settings
- **AiProvider**: Default provider selection and configuration
- **Mcp**: Python path, server script, working directory, auto-install settings
- **ClaudeCli**: Configuration for Claude CLI integration with MCP
- **DesktopAutomation**: Settings for Claude and ChatGPT desktop app automation
- **WebApi**: Base URL and connection settings for the WebApi backend
- **Logging**: File and console logging configuration

#### WebApi Backend (src\BatuLabAiExcel.WebApi\appsettings.json)
- **Database**: SQLite connection string (development), PostgreSQL for production
- **Authentication**: JWT settings, password policies, lockout configuration
- **Stripe**: Payment configuration, webhook secrets, price IDs
- **License**: Trial duration, pricing, validation settings
- **Email**: SMTP configuration for license delivery and notifications
- **Cors**: Cross-origin request settings for admin panel
- **RateLimit**: API rate limiting configuration

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
- **Desktop Application**: `logs/office-ai-batu-lab-{date}.log`
- **WebApi Backend**: `src/BatuLabAiExcel.WebApi/logs/webapi-{date}.log`
- **MCP Communication**: Debug level logging in desktop app logs
- **Authentication Events**: Information level logging in WebApi logs

### Debug Mode
Set `ASPNETCORE_ENVIRONMENT=Development` for:
- Detailed logging output
- Additional error information
- Database auto-migration
- Development-specific configuration

## Project Structure

### Repository Layout
```
D:\excelaioffice\
├── admin-panel\              # React TypeScript admin dashboard
│   ├── src\components\       # React components organized by feature
│   ├── src\services\         # API client and utilities
│   ├── package.json          # Node.js dependencies and scripts
│   └── vite.config.ts        # Vite build configuration
├── src\BatuLabAiExcel\       # Main WPF desktop application
│   ├── Views\                # WPF Windows and UserControls
│   ├── ViewModels\           # MVVM ViewModels with CommunityToolkit
│   ├── Services\             # AI services, MCP client, integrations
│   ├── Models\               # DTOs and domain models
│   └── Infrastructure\       # ProcessHelper, HTTP clients
├── src\BatuLabAiExcel.WebApi\ # ASP.NET Core WebApi backend
│   ├── Controllers\          # REST API controllers
│   ├── Services\             # Business logic services
│   ├── Models\               # Entities, DTOs, configuration
│   ├── Data\                 # Entity Framework DbContext
│   └── Migrations\           # EF Core database migrations
├── scripts\                  # PowerShell automation scripts
├── excel_files\              # Default MCP working directory
└── logs\                     # Application log files
```

### Key Dependencies

#### Desktop Application
- **.NET 9** with WPF and MVVM
- **CommunityToolkit.Mvvm** for source generators
- **Microsoft.Extensions.Hosting** for dependency injection
- **Serilog** for structured logging
- **Polly** for HTTP retry policies
- **System.Text.Json** for JSON serialization

#### WebApi Backend  
- **ASP.NET Core 9** with Entity Framework Core
- **SQLite** for development, PostgreSQL for production
- **Stripe.net** for payment processing
- **MailKit** for SMTP email delivery
- **BCrypt.Net** for password hashing
- **JWT authentication** with rate limiting

#### Admin Panel
- **React 19** with TypeScript
- **Vite** for build tooling
- **TailwindCSS** for styling
- **Axios** for HTTP requests
- **React Hook Form** with Zod validation
- **Recharts** for data visualization
- **React Query** for state management

## Admin Panel Features

The React-based admin panel provides enterprise management capabilities:

### Core Features
- **Dashboard**: System metrics, user growth charts, revenue analytics
- **User Management**: View, edit, create user accounts with role management
- **License Management**: Create, extend, revoke licenses with batch operations
- **Payment Tracking**: Stripe payment history, subscription management
- **Notifications**: Send system-wide or targeted user notifications
- **Analytics**: Usage statistics, performance metrics, error tracking

### Development Workflow
The admin panel is a separate React application that communicates with the WebApi backend via REST APIs. It's designed for system administrators to manage the Office AI platform.

### Authentication Flow
1. Admin logs in via LoginPage component
2. JWT token stored in AuthContext
3. All API requests include Authorization header
4. Token refresh handled automatically
5. Role-based access control for different admin functions

## Important Notes

### Security Considerations
- **API Keys**: Never commit API keys - use environment variables or appsettings
- **JWT Secrets**: Use strong, unique JWT secrets in production
- **Database**: SQLite for development, PostgreSQL with proper backup for production
- **HTTPS**: Always use HTTPS in production for both WebApi and admin panel
- **CORS**: Configure restrictive CORS policies for production

### Performance Tips  
- The MCP server process is managed automatically with restart capabilities
- AI provider selection allows switching between Claude, Gemini, and Groq based on performance needs
- Desktop automation (Claude/ChatGPT desktop apps) is available as fallback option
- Rate limiting prevents API abuse and manages costs

### Monitoring
- Structured logging with Serilog provides detailed operational insights
- Health check scripts verify MCP server and Python environment
- WebApi includes request/response logging middleware
- Admin panel provides real-time system status and error monitoring

# important-instruction-reminders
Do what has been asked; nothing more, nothing less.
NEVER create files unless they're absolutely necessary for achieving your goal.
ALWAYS prefer editing an existing file to creating a new one.
NEVER proactively create documentation files (*.md) or README files. Only create documentation files if explicitly requested by the User.
