# GitHub Copilot prompt — Century Income backend coding

Use the attached files:
- century_income_gmb_factors.csv
- century_income_gsv_factors.csv
- century_income_ssv_factors.csv
- century_income_tables_and_rules.md

Task: implement a backend-only, table-driven Century Income calculation engine. Do not place factor logic in frontend code. Load CSVs into seed/master tables and calculate using backend services.

Requirements:
1. Build lookup repositories for GMB, GSV, SSV Factor 1, and option-specific SSV Factor 2.
2. Input flow must support Life Assured same as Proposer = Yes/No.
3. All age-based calculations must use Life Assured age.
4. Implement formulas for installment premium, guaranteed survival benefit, loyalty survival benefit, maturity, GSV, SSV, surrender value, and death benefit exactly as documented in century_income_tables_and_rules.md.
5. Produce year-wise output from policy year 1 to PT with these columns:
   - Policy Year
   - Annualized Premium
   - Guaranteed Survival Benefit
   - Loyalty Survival Benefit
   - Maturity
   - Death Benefit
   - Guaranteed Surrender Value (GSV) (a)
   - Special Surrender Value (SSV) (b)
   - Surrender Value [Greater of (a) or (b)]
6. Keep rendering separate from calculation. The backend must return a structured year-wise response that PDF generation can consume.
7. Do not use Excel OFFSET logic in code. Replace all Excel formulas with explicit lookup/helper methods.
8. Add tests for all valid PPT/PT combinations and all three options.

Suggested backend methods:
- GetGmbFactor(ageLa, option, ppt, pt)
- GetGsvFactor(policyYear, ppt, pt)
- GetSsvFactor1(policyYear, ppt, pt)
- GetSsvFactor2(option, policyYear, ppt, pt)
- CalculateInstallmentPremium(input)
- CalculateGuaranteedSurvivalBenefit(input, policyYear)
- CalculateLoyaltySurvivalBenefit(input, policyYear)
- CalculateMaturity(input, policyYear)
- CalculateGsv(input, policyYear, cumulativeGuaranteed, cumulativeLoyalty)
- CalculateSsv(input, policyYear, maturityBenefit, guaranteedSurvivalBenefit, loyaltySurvivalBenefit)
- CalculateSurrenderValue(gsv, ssv)
- CalculateDeathBenefit(input, policyYear, surrenderValue)
- GenerateBenefitIllustration(input)

Output expectations:
- GSV and SSV must not be blank once calculable.
- Maturity only appears in policy year = PT.
- Twin Income survival benefits only appear in scheduled years.
- Deferred and Twin loyalty survival benefits must be zero.
- PDF mapping should consume backend output fields directly.
