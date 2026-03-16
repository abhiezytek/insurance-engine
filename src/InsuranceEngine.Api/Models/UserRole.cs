namespace InsuranceEngine.Api.Models;

public class UserRole
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }

    public UserMaster User { get; set; } = null!;
    public RoleMaster Role { get; set; } = null!;
}
