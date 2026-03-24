using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Services;

public class NotificationService : INotificationService
{
    private readonly InsuranceDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(InsuranceDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Notification>> GetUnread(string userId)
    {
        return await _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task MarkAsRead(int notificationId, string userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification != null)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsRead(string userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        await _db.SaveChangesAsync();
    }

    public async Task CreateAsync(string userId, string message, string? relatedModule = null, string? relatedId = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message,
            RelatedModule = relatedModule,
            RelatedId = relatedId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task NotifyRoleAsync(string roleName, string message, string? relatedModule = null, string? relatedId = null)
    {
        // Find all users with the specified role
        var usersInRole = await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.Role != null && ur.Role.RoleName == roleName)
            .Select(ur => ur.User != null ? ur.User.Email : null)
            .Where(u => u != null)
            .ToListAsync();

        foreach (var username in usersInRole)
        {
            if (string.IsNullOrEmpty(username)) continue;
            _db.Notifications.Add(new Notification
            {
                UserId = username,
                Message = message,
                RelatedModule = relatedModule,
                RelatedId = relatedId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (usersInRole.Count > 0)
            await _db.SaveChangesAsync();

        _logger.LogInformation("Notified {Count} users with role {Role}: {Message}", usersInRole.Count, roleName, message);
    }
}
