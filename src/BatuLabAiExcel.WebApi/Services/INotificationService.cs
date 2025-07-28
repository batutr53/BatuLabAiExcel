using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.Entities;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// Notification service interface
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send a notification to a specific user.
    /// </summary>
    Task<Result> SendNotificationAsync(Guid userId, string title, string message, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast a notification to all active users.
    /// </summary>
    Task<Result> BroadcastNotificationAsync(string title, string message, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notifications for a specific user.
    /// </summary>
    Task<Result<List<Notification>>> GetUserNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all notifications (for admin panel).
    /// </summary>
    Task<Result<List<Notification>>> GetAllNotificationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a notification as read.
    /// </summary>
    Task<Result> MarkNotificationAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
}