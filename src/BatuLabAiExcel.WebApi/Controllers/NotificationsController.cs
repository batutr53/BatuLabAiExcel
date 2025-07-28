using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Services;
using System.Security.Claims;

namespace BatuLabAiExcel.WebApi.Controllers;

/// <summary>
/// Notifications controller for user-specific notifications
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get notifications for the current user
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<List<ApiNotification>>>> GetMyNotifications()
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<List<ApiNotification>>.ErrorResult("Invalid user token"));
            }

            var result = await _notificationService.GetUserNotificationsAsync(userId);
            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<List<ApiNotification>>.ErrorResult(result.Error ?? "Failed to retrieve notifications"));
            }

            var apiNotifications = result.Value.Select(n => new ApiNotification
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            }).ToList();

            return Ok(ApiResponse<List<ApiNotification>>.SuccessResult(apiNotifications));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for current user");
            return StatusCode(500, ApiResponse<List<ApiNotification>>.ErrorResult("Failed to retrieve notifications"));
        }
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<ActionResult<ApiResponse>> MarkNotificationAsRead(Guid id)
    {
        try
        {
            var result = await _notificationService.MarkNotificationAsReadAsync(id);
            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.ErrorResult(result.Error ?? "Failed to mark notification as read"));
            }
            return Ok(ApiResponse.SuccessResult("Notification marked as read"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return StatusCode(500, ApiResponse.ErrorResult("Failed to mark notification as read"));
        }
    }
}