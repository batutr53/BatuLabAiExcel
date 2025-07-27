namespace BatuLabAiExcel.WebApi.Models;

/// <summary>
/// Rate limiting configuration settings
/// </summary>
public class RateLimitSettings
{
    public bool EnableRateLimiting { get; set; } = true;
    public int GeneralLimit { get; set; } = 100;
    public int WindowInMinutes { get; set; } = 1;
    public int AuthLimit { get; set; } = 10;
    public int PaymentLimit { get; set; } = 5;
}