namespace InsuranceEngine.Api.Models;

public class LoginHistory
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    public DateTime? LogoutTime { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}
