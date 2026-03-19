# GitHub Copilot prompt — code review and output redesign for Century Income, eWealth Royale, and YPYG

Use the workbook, annexure, and earlier generated CSV/MD files together as the source-of-truth reference set. Review the current codebase, identify formula drift from the business files, fix the calculation engine, and redesign YPYG output. [file:42][file:43][file:44][file:45][file:24][file:55][file:54][file:65][file:66][code_file:56][code_file:57][code_file:58][code_file:59][code_file:60][code_file:62][code_file:63]

## Review scope

Review three modules in the current codebase: [code_file:65]
- Century Income calculation and output generation. [file:42][file:24][file:43][file:44][file:45]
- eWealth Royale calculation, validations, and illustration output. [file:55][file:54][code_file:56][code_file:57][code_file:58][code_file:59][code_file:60][code_file:62][code_file:63]
- YPYG output layout redesign using the attached traditional and ULIP reference formats. [file:65][file:66]

## Main objective

Do a code review against business rules, not just a UI cleanup. Compare the current formulas and mappings in code with the attached BI files, annexures, F&U files, and generated master CSV/MD files. Wherever the current code calculation does not match the business source, correct the code and document the mismatch in a review note or pull-request style summary. [file:42][file:24][file:55][file:54]

## Century Income checks

Century Income is a non-linked, non-participating savings product with fixed PPT/PT combinations and benefit rules that vary by option. [file:24] Restrict Premium Payment Term to 7, 10, and 12 years, and restrict Policy Term to the combinations allowed in the F&U and BI workbook: 7 -> 15 or 20; 10 -> 20 or 25; 12 -> 25. [file:24][file:42]

Review and correct the following formula areas in the current code: [file:24][file:42][file:43][file:44][file:45]
- GMB factor lookup by age, PPT, PT, and option using Annexure 1 / BI structure. [file:42][file:43]
- Guaranteed Maturity Benefit calculation including high-premium and channel/staff enhancements where applicable. [file:24][file:42]
- Guaranteed Surrender Value formula: GSV factor x total premiums paid till date of surrender minus survival benefits already paid. [file:24][file:45]
- Special Surrender Value formula: SSV Factor 1 x paid-up maturity benefit + SSV Factor 2 x guaranteed income or reduced paid-up loyalty income as applicable by option. [file:24][file:44]
- Reduced paid-up calculations for death, maturity, and survival benefits using number of premiums paid divided by number of premiums payable. [file:24]
- Death benefit floor logic including 105% of total premiums paid where applicable in product wording. [file:24]

Do not leave these as fragmented formulas across controllers, screens, and PDF builders. Centralize them into domain services or calculation modules with explicit method names such as `CalculateCenturyIncomeMaturity`, `CalculateCenturyIncomeSurrender`, `CalculateCenturyIncomeReducedPaidUpBenefits`, and `BuildCenturyIncomeIllustrationRows`. [file:24][file:42]

## eWealth Royale checks

eWealth Royale is a ULIP and the yearly illustration must be consistent between Part B detail rows and Part A summary rows. The code must produce separate 4% and 8% gross return streams and then derive Part A from the corresponding Part B yearly detail rows, not by using a second independent formula path. [file:55][code_file:57]

Review and correct the following areas: [file:55][file:54][code_file:56][code_file:57][code_file:58][code_file:59][code_file:60][code_file:62][code_file:63]
- Type of PPT must be table-driven with Single, Limited, and Regular. [code_file:57][code_file:60]
- PPT and PT must be linked dropdowns based on plan option and minor/major rules. [code_file:57][code_file:60]
- DOB must be the only age input and age must be derived automatically. [code_file:57]
- Life Assured and Proposer same-person flow must auto-copy proposer data. [code_file:57]
- Self-managed fund allocation must total exactly 100 and each percent must be in multiples of 5. [code_file:57][code_file:58]
- Risk Preference must remain only for Age-based Investment Strategy and must map Aggressive/Conservative allocation between Blue Chip Equity Fund and Gilt Fund by attained age. [file:54][code_file:62][code_file:63]
- Mortality Charges, Additional Risk Benefit Charges, Other Charges, Fund at the End of the Year, Surrender Value, and Death Benefit must map from yearly detail rows. [file:55][code_file:57]
- FMC and mortality rates must come from master tables or CSV seeds, not hardcoded constants. [code_file:56][code_file:59]

Use explicit calculation objects, for example `EwealthYearlyDetailRow`, `EwealthSummaryRow`, `FundAllocationRule`, and `PptPtRule`. Make the screen validations and PDF labels backend-driven from these rules. [code_file:57][code_file:60][code_file:63]

## YPYG redesign

Redesign the YPYG output using the attached reference layouts as visual direction, but simplify the business content drastically. The current requirement is to show only three business values in the YPYG output: current date survival benefit, current maturity value, and value if premiums are paid till the end of the term. [file:65][file:66]

Build a compact `Policy at a Glance` style output card or PDF section with these fields only: [file:65][file:66]
- Current Date. [file:65]
- Current Date Survival Benefit. [file:65]
- Current Maturity Value. [file:65][file:66]
- Value if Paid Till End of Term. [file:65][file:66]

Do not show full illustration grids, yearly projections, charge breakup tables, extra policy servicing data, revival amount, unclaimed fund, bonus accrued, or other non-essential data in the new YPYG output unless required elsewhere in the app. The aim is a clean summary layout inspired by the attached files, not a dense benefit illustration. [file:65][file:66]

## Expected code review output

While reviewing the code, produce these deliverables inside the repo: [code_file:65]
- A review summary markdown file listing each mismatch found between code and business files. [file:42][file:24][file:55][file:54]
- Refactored calculation services for Century Income and eWealth Royale. [file:42][file:55]
- Unit tests for sample scenarios taken from the BI / annexure structures. [file:42][file:43][file:44][file:45][file:55]
- A redesigned YPYG output component/template and PDF renderer section matching the simplified requirement. [file:65][file:66]

## Required implementation behavior

1. Do not hardcode workbook cell addresses in production code. Convert Excel logic into clear domain formulas and master tables. [file:42][file:55]
2. Keep all business masters externalized in CSV, DB tables, or strongly typed seeded configuration. [code_file:56][code_file:58][code_file:59][code_file:60][code_file:62]
3. Add field-level validation and highlight mandatory missing inputs instead of failing silently. [code_file:57]
4. Keep display labels business-friendly and avoid abbreviations in PDFs where the earlier requirement asked for full wording. [code_file:57]
5. Preserve statutory content where legally required, but simplify business-facing YPYG output to the new minimal format. [file:65][file:66]

## Suggested implementation tasks

- Review current formulas line by line against business files. [file:42][file:24][file:55][file:54]
- Create or update master readers for Century Income and eWealth tables. [file:43][file:44][file:45][code_file:56][code_file:58][code_file:59][code_file:60][code_file:62]
- Refactor calculation services and remove duplicated formulas from UI/PDF layers. [file:42][file:55]
- Add automated tests for 4 to 6 representative scenarios per product. [file:42][file:55]
- Redesign YPYG summary component and PDF section to only show the three requested values. [file:65][file:66]

## Final instruction to Copilot

Treat this as a code-review plus refactor assignment. First identify where the current code differs from the attached business sources, then fix the formulas, move constants into master data, add tests, and redesign YPYG output to the simplified attached-style summary. Do not stop at cosmetic UI updates. [file:42][file:24][file:55][file:54][file:65][file:66]
