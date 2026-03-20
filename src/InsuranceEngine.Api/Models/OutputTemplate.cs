namespace InsuranceEngine.Api.Models;

public class OutputTemplate
{
    public int Id { get; set; }
    public int ProductVersionId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string OutputFormat { get; set; } = "PDF";
    public string TemplateJson { get; set; } = "{}";
}
