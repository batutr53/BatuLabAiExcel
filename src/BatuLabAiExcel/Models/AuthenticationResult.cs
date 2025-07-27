namespace BatuLabAiExcel.Models;

/// <summary>
/// Authentication result from Web API
/// </summary>
public class AuthenticationResult
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Token { get; set; }
}