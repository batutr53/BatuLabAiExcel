using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json;
using BatuLabAiExcel.WebApi.Data;
using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.Entities;
using BatuLabAiExcel.WebApi.Services;
using BatuLabAiExcel.WebApi.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/webapi-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Method to seed admin user
static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
{
    var context = serviceProvider.GetRequiredService<AppDbContext>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    // Check if admin user already exists
    var adminEmail = "admin@batulab.com";
    var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
    
    if (existingAdmin == null)
    {
        // Create admin user
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // Default password: admin123
            IsEmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Create lifetime license for admin
        var adminLicense = new License
        {
            Id = Guid.NewGuid(),
            UserId = adminUser.Id,
            Type = LicenseType.Lifetime,
            Status = LicenseStatus.Active,
            LicenseKey = $"ADMIN-{Guid.NewGuid().ToString("N")[..16].ToUpper()}",
            IsActive = true,
            StartDate = DateTime.UtcNow,
            ExpiresAt = null, // Lifetime license never expires
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        context.Licenses.Add(adminLicense);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Admin user created with email: {Email} and password: admin123", adminEmail);
    }
    else
    {
        logger.LogInformation("Admin user already exists: {Email}", adminEmail);
    }
}

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        });

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Office AI - Batu Lab Web API",
            Version = "v1",
            Description = "Secure backend API for Office AI - Batu Lab desktop application"
        });

        // Add JWT authentication to Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Database
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetSection("Database")["ConnectionString"]));

    // Configuration sections
    builder.Services.Configure<AppConfiguration.DatabaseSettings>(
        builder.Configuration.GetSection("Database"));
    builder.Services.Configure<AppConfiguration.AuthenticationSettings>(
        builder.Configuration.GetSection("Authentication"));
    builder.Services.Configure<AppConfiguration.LicenseSettings>(
        builder.Configuration.GetSection("License"));
    builder.Services.Configure<AppConfiguration.StripeSettings>(
        builder.Configuration.GetSection("Stripe"));
    builder.Services.Configure<AppConfiguration.EmailSettings>(
        builder.Configuration.GetSection("Email"));
    builder.Services.Configure<CorsSettings>(
        builder.Configuration.GetSection("Cors"));
    builder.Services.Configure<RateLimitSettings>(
        builder.Configuration.GetSection("RateLimit"));

    // Add business services
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    builder.Services.AddScoped<ILicenseService, LicenseService>();
    builder.Services.AddScoped<IPaymentService, PaymentService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IApiAuthenticationService, ApiAuthenticationService>();
    builder.Services.AddScoped<IUserManagementService, UserManagementService>();

    // JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("Authentication");
    var key = Encoding.ASCII.GetBytes(jwtSettings["JwtSecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured"));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    // CORS
    var corsSettings = builder.Configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultPolicy", policy =>
        {
            // More permissive CORS for development
            if (builder.Environment.IsDevelopment())
            {
                policy.SetIsOriginAllowed(origin => 
                    origin.StartsWith("http://localhost") || 
                    origin.StartsWith("https://localhost"))
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
            else
            {
                policy.WithOrigins(corsSettings.AllowedOrigins)
                      .WithMethods(corsSettings.AllowedMethods)
                      .WithHeaders(corsSettings.AllowedHeaders)
                      .AllowCredentials();
            }
        });
    });

    // Rate Limiting
    var rateLimitSettings = builder.Configuration.GetSection("RateLimit").Get<RateLimitSettings>() ?? new RateLimitSettings();
    if (rateLimitSettings.EnableRateLimiting)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("General", opt =>
            {
                opt.PermitLimit = rateLimitSettings.GeneralLimit;
                opt.Window = TimeSpan.FromMinutes(rateLimitSettings.WindowInMinutes);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
            });

            options.AddFixedWindowLimiter("Auth", opt =>
            {
                opt.PermitLimit = rateLimitSettings.AuthLimit;
                opt.Window = TimeSpan.FromMinutes(rateLimitSettings.WindowInMinutes);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });

            options.AddFixedWindowLimiter("Payment", opt =>
            {
                opt.PermitLimit = rateLimitSettings.PaymentLimit;
                opt.Window = TimeSpan.FromMinutes(rateLimitSettings.WindowInMinutes);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 2;
            });
        });
    }

    var app = builder.Build();

    // Configure the HTTP request pipeline
    // Enable Swagger in all environments for now (change in production)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Office AI - Batu Lab Web API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });

    app.UseHttpsRedirection();

    app.UseCors("DefaultPolicy");

    // Rate limiting
    if (rateLimitSettings.EnableRateLimiting)
    {
        app.UseRateLimiter();
    }

    // Custom middleware
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Auto-migrate database
    var databaseSettings = builder.Configuration.GetSection("Database").Get<AppConfiguration.DatabaseSettings>();
    if (databaseSettings?.EnableAutoMigration == true)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        Log.Information("Database migration completed");
        
        // Seed admin user
        await SeedAdminUserAsync(scope.ServiceProvider);
    }

    Log.Information("Starting Office AI - Batu Lab Web API");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}