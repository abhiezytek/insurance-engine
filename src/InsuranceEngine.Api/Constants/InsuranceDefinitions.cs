namespace InsuranceEngine.Api.Constants;

/// <summary>Abbreviations and constants for the Endowment (formerly Century Income) product.</summary>
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
    public const string SAD = "SAD";  // Sum Assured on Death (Endowment: MAX(10×AP, SAM); ULIP: MAX(SA-PW,FV,105%×TPP))
    public const string SAR = "SAR";  // Sum At Risk (for Mortality calculation)
    public const string NAV = "NAV";  // Net Asset Value (for ULIP fund value)
    public const string RPU = "RPU";  // Reduced Paid-Up status flag
    public const string TPP = "TPP";  // Total Premiums Paid
    public const string T = "t";      // Number of Premiums Paid
    public const string N = "n";      // Number of Premiums Payable (= PPT)
}

