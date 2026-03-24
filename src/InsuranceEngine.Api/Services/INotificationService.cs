using InsuranceEngine.Api.Models;

namespace InsuranceEngine.Api.Services;

public interface INotificationService
{
    Task<List<Notification>> GetUnread(string userId);
    Task MarkAsRead(int notificationId, string userId);
    Task MarkAllAsRead(string userId);
    Task CreateAsync(string userId, string message, string? relatedModule = null, string? relatedId = null);
    Task NotifyRoleAsync(string roleName, string message, string? relatedModule = null, string? relatedId = null);
}
