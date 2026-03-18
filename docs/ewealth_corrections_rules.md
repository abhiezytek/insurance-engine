# eWealth Royale correction rules for GitHub coding

Use the attached CSV files as backend master data and use the BI workbook and F&U only as rule references. The UI must be data-driven and the calculation engine must stay separate from the screen layer. [file:55][file:54]

## Files
- `ewealth_ppt_pt_rules.csv` — valid Type of PPT, PPT, PT combinations by plan option and Platinum Plus minor/major condition. [file:54]
- `ewealth_fmc_factors.csv` — fund management charge master from Charges_Commisison sheet. [code_file:56][file:55]
- `ewealth_mortality_factors.csv` — mortality charge rates and ATPD rates by age from Charges_Commisison sheet. [file:55]
- `ewealth_fund_master.csv` — fund labels captured from Input sheet for dropdown binding. [file:55]

## UI corrections
1. Type of PPT must have options Single, Limited, Regular and be table-driven. [file:54][file:55]
2. PT and PPT must be linked dropdowns and not independent input fields. [file:54][file:55]
3. Screen flow should be: select plan option, then Type of PPT, then PPT, then PT. [file:54][file:55]
4. Use only DOB, auto-calculate age, do not ask for age input. [file:55]
5. Ask once if Life Assured and Proposer are same. If yes, no duplicate proposer re-entry. [file:55]
6. Arrange form in boxed sections with small headers: Life Assured Details, Proposer Details, Plan Details, Premium Details, Fund Details. [file:55]
7. Remove unwanted fields from UI and request model: EMR classes, flat extra inputs, Kerala Flood Cess, Age at Risk Commencement, Risk Preference, Policy Effective Date. [file:55]
8. Remove SUD Life product wording from app screen/output where business asked, but preserve mandatory regulatory footer/warnings in final PDF template. [file:27][file:55]

## Fund allocation rules
- Self-managed fund allocation must total exactly 100. [file:55]
- Total cannot be below or above 100. [file:55]
- Each fund percent must be in multiples of 5. [file:55]
- Fund dropdowns should come from master file and include the 12 funds visible in the workbook input sheet. [file:55]

## Illustration button behavior
`Generate Illustration` must validate and highlight missing mandatory fields, or scroll to invalid fields and mark them. It must not silently fail. [file:55]

## Display labels
Use full labels, not abbreviations: Mortality Charges, Additional Risk Benefit Charges, Other Charges, Goods and Services Tax, Fund at the End of the Year, Surrender Value, Death Benefit. [file:55]

## Calculation and mapping corrections
The BI workbook has separate 4% and 8% streams in Part A and Part B. [file:55]
Part A must be derived from Part B yearly rows by policy year, not re-computed separately. [file:55]
The business-provided VLOOKUP formulas indicate Part A columns map from the Part B detail row for the same policy year. [file:55]

Map yearly summary fields from detail rows:
- Mortality Charges <- yearly detail mortality charges. [file:55]
- Additional Risk Benefit Charges <- yearly detail additional risk benefit charges. [file:55]
- Other Charges <- yearly detail policy administration charges + FMC + workbook-mapped other components. [file:55]
- Fund at the End of the Year <- yearly detail fund at end. [file:55]
- Surrender Value <- yearly detail surrender value. [file:55]
- Death Benefit <- yearly detail death benefit. [file:55]

## Rate master notes
- Policy administration charge is 1200 p.a. up to 10 years in the workbook master. [file:55]
- FMC varies by fund and is loaded from the charges sheet. [code_file:56][file:55]
- Mortality and ATPD rates vary by age and are loaded from the charges sheet. [file:55]
- Minimum death benefit is 105% of total premiums paid. [file:54][file:55]

## PDF note
The current ULIP format contains 4% maturity wording and a separate 7% to 8% note, so projection wording should be configurable and not hardcoded in UI logic. [file:27]
