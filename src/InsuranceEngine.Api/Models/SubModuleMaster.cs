namespace InsuranceEngine.Api.Models;

public class SubModuleMaster
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string SubModuleName { get; set; } = string.Empty;
    public string SubModuleCode { get; set; } = string.Empty;

    public ModuleMaster Module { get; set; } = null!;
}
