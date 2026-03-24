using InsuranceEngine.Api.Models;

namespace InsuranceEngine.Api.Services;

public interface INotificationService
{
    Task<(List<Notification> Data, int TotalCount)> GetUnread(string userId, int page = 1, int pageSize = 20);
    Task MarkAsRead(int notificationId, string userId);
    Task MarkAllAsRead(string userId);
    Task CreateAsync(string userId, string message, string? relatedModule = null, string? relatedId = null);
    Task NotifyRoleAsync(string roleName, string message, string? relatedModule = null, string? relatedId = null);
}
