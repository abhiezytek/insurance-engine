namespace InsuranceEngine.Api.DTOs;

public class CreateProductRequest
{
    public int InsurerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ProductType { get; set; } = "Traditional";
}

public class CreateProductVersionRequest
{
    public int ProductId { get; set; }
    public string Version { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveDate { get; set; }
}

public class CreateParameterRequest
{
    public int ProductVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = "decimal";
    public bool IsRequired { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
}

public class CreateFormulaRequest
{
    public int ProductVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; }
    public string? Description { get; set; }
}

public class UpdateFormulaRequest
{
    public string Name { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; }
    public string? Description { get; set; }
}

public class CreateConditionGroupRequest
{
    public int ProductVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LogicalOperator { get; set; } = "AND";
    public int? ParentGroupId { get; set; }
}

public class CreateConditionRequest
{
    public int ConditionGroupId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Value2 { get; set; }
}
