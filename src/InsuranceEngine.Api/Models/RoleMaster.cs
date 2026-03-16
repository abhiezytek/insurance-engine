namespace InsuranceEngine.Api.Models;

public class RoleMaster
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public List<RoleModuleAccess> ModuleAccess { get; set; } = new();
}
