# GitHub Copilot prompt — eWealth correction using CSV masters

Use these files:
- ewealth_ppt_pt_rules.csv
- ewealth_fmc_factors.csv
- ewealth_mortality_factors.csv
- ewealth_fund_master.csv
- ewealth_corrections_rules.md

Implement corrections in a backend-driven way.

Requirements:
1. Type of PPT options must be Single, Limited, Regular from the rules table.
2. PT and PPT must be linked dropdowns based on selected plan option, PPT type, and minor/major logic for Platinum Plus.
3. Replace age inputs with DOB and calculate age automatically.
4. Ask whether Life Assured and Proposer are same; if yes, auto-copy proposer values and hide duplicate inputs.
5. Build screen sections with boxed layout and section headers.
6. Remove unwanted fields from UI and backend request model.
7. Enforce self-managed fund allocation total = 100 and each fund % multiple of 5.
8. Use fund master CSV for dropdown options.
9. Generate Illustration must show mandatory field validation and highlight invalid fields.
10. Use full display labels, no abbreviations.
11. Build yearly detail rows for both 4% and 8% projections.
12. Build Part A summary from Part B detail rows by policy year.
13. Load FMC and mortality rates from CSV masters instead of frontend constants.
14. Keep screen branding suppressed per business request, but preserve statutory footer/warnings in final regulated PDF.
15. Do not use Excel VLOOKUP or cell references in code. Convert workbook structures into master tables and explicit backend methods.
