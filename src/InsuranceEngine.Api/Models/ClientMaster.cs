namespace InsuranceEngine.Api.Models;

public class ClientMaster
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public List<ClientModuleAccess> ModuleAccess { get; set; } = new();
}
