namespace InsuranceEngine.Api.Models;

/// <summary>Application user for JWT-based authentication.</summary>
public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool ForceChangePassword { get; set; }
    public DateTime? PasswordChangedOn { get; set; }
}
