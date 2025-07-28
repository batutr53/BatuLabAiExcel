using BatuLabAiExcel.WebApi.Data;
using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// Notification service implementation
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> SendNotificationAsync(Guid userId, string title, string message, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, title);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            return Result.Failure("Failed to send notification");
        }
    }

    public async Task<Result> BroadcastNotificationAsync(string title, string message, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            // For simplicity, this will create a notification for each active user.
            // In a real-world scenario, you might use a dedicated notification system
            // or a single notification record with a 'broadcast' flag.
            var activeUserIds = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            var notifications = activeUserIds.Select(userId => new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Broadcast notification sent to {Count} users: {Title}", notifications.Count, title);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification");
            return Result.Failure("Failed to broadcast notification");
        }
    }

    public async Task<Result<List<Notification>>> GetUserNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);

            return Result<List<Notification>>.Success(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            return Result<List<Notification>>.Failure("Failed to retrieve notifications");
        }
    }

    public async Task<Result<List<Notification>>> GetAllNotificationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);

            return Result<List<Notification>>.Success(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all notifications");
            return Result<List<Notification>>.Failure("Failed to retrieve all notifications");
        }
    }

    public async Task<Result> MarkNotificationAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                return Result.Failure("Notification not found");
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Notification {NotificationId} marked as read", notificationId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            return Result.Failure("Failed to mark notification as read");
        }
    }
}