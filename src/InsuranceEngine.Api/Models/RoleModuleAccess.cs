namespace InsuranceEngine.Api.Models;

public class RoleModuleAccess
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int ModuleId { get; set; }
    public int? SubModuleId { get; set; }
    public bool CanView { get; set; } = false;
    public bool CanExecute { get; set; } = false;
    public bool CanApprove { get; set; } = false;
    public bool CanDownload { get; set; } = false;
    public bool CanUpload { get; set; } = false;
    public bool CanAdmin { get; set; } = false;

    public RoleMaster Role { get; set; } = null!;
    public ModuleMaster Module { get; set; } = null!;
    public SubModuleMaster? SubModule { get; set; }
}
