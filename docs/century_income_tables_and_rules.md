# Century Income tables and coding rules

Use the attached CSV files as the only factor sources for Century Income calculations. These factors come from the uploaded annexure workbooks and must be loaded into backend tables or seeded master data, not embedded in frontend code. [file:43][file:44][file:45]

## CSV files

- `century_income_gmb_factors.csv` — GMB lookup by age of life assured, option, PPT, PT. [file:43]
- `century_income_gsv_factors.csv` — GSV factor lookup by policy year, PPT, PT. [file:45]
- `century_income_ssv_factors.csv` — SSV Factor 1 and option-specific SSV Factor 2 lookup by policy year, PPT, PT. [file:44]

## Allowed combinations

Allowed PPT/PT combinations are 7/15, 7/20, 10/20, 10/25, and 12/25. [file:24][file:42]

Option codes should map as:
- 1 = Immediate Income [file:42][file:24]
- 2 = Deferred Income [file:42][file:24]
- 3 = Twin Income [file:42][file:24]

## Input flow

Ask first whether Life Assured and Proposer are the same person. If yes, capture only LA name, age, and gender, then copy those values to proposer fields. If no, capture separate LA and proposer names, ages, and genders. All Century Income calculations must use Life Assured age for age-based factor lookup. [file:24][file:43]

## Core formulas

### Installment premium

Use:
`ROUND(annualizedPremium * modalFactor + modalFactor * (standardAgeProof == "No" ? 1.5 * sumAssured / 1000 : 0), 0)` [file:42]

Mode factors:
- Yearly = 1 [file:42]
- Half Yearly = 0.5108 [file:42]
- Quarterly = 0.2582 [file:42]
- Monthly = 0.0867 [file:42]

### Sum assured on death

Use `10 * annualPremium`, where annual premium is premium payable in a year excluding taxes, rider premiums, and underwriting extra premiums. [file:24]

### Maturity benefit

Use `annualizedPremium * gmbFactor`, where `gmbFactor` is looked up from `century_income_gmb_factors.csv` using life assured age, option, PPT, and PT. [file:24][file:43]

### Guaranteed survival benefit

- Immediate Income: starts from end of policy year 1 at 10% of annualized premium and continues yearly till end of PT. [file:24]
- Deferred Income: starts one year after end of PPT and follows year-wise GI schedule from F&U / BI. [file:24][file:42]
- Twin Income: 105% of annualized premium in only the scheduled years for the selected PPT/PT combination. [file:24][file:42]

### Loyalty survival benefit

Only for Immediate Income. It starts at end of policy year 2 and follows the option schedule from the F&U by PPT and policy year. Deferred and Twin loyalty survival benefit are zero for all years. [file:24][file:42]

### GSV

Use:
`max(0, premiumsPaidToDate * gsvFactor - cumulativeGuaranteedSurvivalBenefit - cumulativeLoyaltySurvivalBenefit)` [file:24][file:45]

Where:
- `premiumsPaidToDate = annualizedPremium * min(policyYear, ppt)` [file:42][file:45]
- `gsvFactor` comes from `century_income_gsv_factors.csv` by policy year, PPT, PT. [file:45]

### SSV

Use F&U structure:
`SSV = SSV_FACTOR_1 * paidUpMaturityBenefit + SSV_FACTOR_2 * benefitAtInceptionComponent` [file:24][file:44]

Where:
- `paidUpMaturityBenefit = (min(policyYear, ppt) / ppt) * maturityBenefit` [file:24]
- `SSV_FACTOR_1` comes from `century_income_ssv_factors.csv` where `factor_type = SSV_FACTOR_1`. [file:44]
- `SSV_FACTOR_2` comes from `century_income_ssv_factors.csv` where `factor_type = SSV_FACTOR_2` and `option_name` matches the selected option. [file:44]
- For Immediate Income, `benefitAtInceptionComponent = (min(policyYear, ppt) / ppt) * (guaranteedSurvivalBenefit + loyaltySurvivalBenefit)` following workbook structure. [file:42][file:44]
- For Deferred and Twin, `benefitAtInceptionComponent = (min(policyYear, ppt) / ppt) * guaranteedSurvivalBenefit`. [file:42][file:44]

### Surrender value

Use `max(gsv, ssv)`. [file:24][file:42]

### Death benefit

Use:
`max(sumAssuredOnDeath, surrenderValue, 1.05 * annualizedPremium * min(policyYear, ppt))` [file:24][file:42]

### Maturity column in output

Use zero for all years before PT and maturity benefit only in policy year equal to PT. [file:42]

## Output columns

Generate the BI output table in this exact order: [file:42]
- Policy Year
- Annualized Premium
- Guaranteed Survival Benefit
- Loyalty Survival Benefit
- Maturity
- Death Benefit
- Guaranteed Surrender Value (GSV) (a)
- Special Surrender Value (SSV) (b)
- Surrender Value [Greater of (a) or (b)]

## PDF mapping

Map PDF fields from the computed BI output, not from frontend form labels. GSV and SSV must be populated from the calculations and must not be left blank. [file:42]
