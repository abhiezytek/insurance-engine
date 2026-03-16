namespace InsuranceEngine.Api.Models;

public class ClientModuleAccess
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int ModuleId { get; set; }
    public bool Licensed { get; set; } = true;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public ClientMaster Client { get; set; } = null!;
    public ModuleMaster Module { get; set; } = null!;
}
