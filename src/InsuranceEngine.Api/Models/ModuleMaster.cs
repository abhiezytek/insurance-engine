namespace InsuranceEngine.Api.Models;

public class ModuleMaster
{
    public int Id { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public List<SubModuleMaster> SubModules { get; set; } = new();
}
