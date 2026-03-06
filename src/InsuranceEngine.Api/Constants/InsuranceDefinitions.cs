namespace InsuranceEngine.Api.Constants;

/// <summary>Abbreviations and constants for SUD Life Century Income product.</summary>
public static class InsuranceDefinitions
{
    public const string AP = "AP";    // Annual Premium (excl. taxes, rider, extra)
    public const string ANP = "ANP";  // Annualized Premium
    public const string PPT = "PPT";  // Premium Payment Term
    public const string PT = "PT";    // Policy Term
    public const string PY = "PY";    // Policy Year
    public const string GI = "GI";    // Guaranteed Income
    public const string LI = "LI";    // Loyalty Income
    public const string GMB = "GMB";  // Guaranteed Maturity Benefit
    public const string GSV = "GSV";  // Guaranteed Surrender Value
    public const string SSV = "SSV";  // Special Surrender Value
    public const string SV = "SV";    // Surrender Value (= MAX(GSV, SSV))
    public const string SA = "SA";    // Sum Assured
    public const string SAD = "SAD";  // Sum Assured on Death (= 10 × AP)
    public const string TPP = "TPP";  // Total Premiums Paid
    public const string T = "t";      // Number of Premiums Paid
    public const string N = "n";      // Number of Premiums Payable (= PPT)
}
