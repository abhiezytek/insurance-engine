namespace InsuranceEngine.Api.Models;

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RelatedModule { get; set; }
    public string? RelatedId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
