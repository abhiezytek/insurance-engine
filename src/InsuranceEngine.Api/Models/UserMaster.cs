namespace InsuranceEngine.Api.Models;

public class UserMaster
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? EmployeeId { get; set; }
    public string? Department { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public bool ForceChangePassword { get; set; } = true;
    public int? ClientId { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<UserRole> UserRoles { get; set; } = new();
}
