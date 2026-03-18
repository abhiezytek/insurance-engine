# eWealth risk preference rules and mapping

Use this file together with `ewealth_corrections_rules.md`, `ewealth_fund_master.csv`, and `ewealth_ppt_pt_rules.csv` so the UI and backend remain table-driven. [code_file:57][code_file:58][code_file:60]

## Field behavior

Risk Preference is required only when the selected Investment Strategy is `Age-based Investment Strategy`. The F&U states that under Age-based Investment Strategy the policyholder must choose either `Aggressive` or `Conservative`, and the company allocates money between Blue Chip Equity Fund and Gilt Fund according to attained age and chosen preference. [file:54]

Risk Preference must not be shown for `Self-Managed Investment Strategy`, because in that strategy the policyholder directly selects fund allocation percentages instead of using age-based allocation. [file:54][file:55]

## UI rule

Use this flow:
1. Select Plan Option. [file:54][file:55]
2. Select Type of PPT. [file:54][file:55]
3. Select PPT. [file:54][file:55]
4. Select PT. [file:54][file:55]
5. Select Investment Strategy. [file:54][file:55]
6. If strategy = `Age-based Investment Strategy`, show Risk Preference dropdown with values `Aggressive` and `Conservative`. [file:54]
7. If strategy = `Self-Managed Investment Strategy`, hide Risk Preference and instead show fund allocation grid. [file:54][file:55]

## Mapping rule

- `InvestmentStrategy = Age-based Investment Strategy` and `RiskPreference = Aggressive` => use age-based fund allocation logic for Aggressive profile. [file:54]
- `InvestmentStrategy = Age-based Investment Strategy` and `RiskPreference = Conservative` => use age-based fund allocation logic for Conservative profile. [file:54]
- `InvestmentStrategy = Self-Managed Investment Strategy` => `RiskPreference = null` or `Not Applicable` in backend DTO and storage. [file:54][file:55]

## Formula / allocation logic reference

The F&U says that under Age-based Investment Strategy the portfolio is allocated between Blue Chip Equity Fund and Gilt Fund based on attained age and chosen risk preference, and the allocation percentages are shown in the policy document tables. [file:54] Therefore the implementation must not hardcode frontend percentages; it must load age-band allocation rules from backend master data once those age-band tables are seeded. [file:54]

Until the age-band allocation table is seeded, use these implementation placeholders:
- `GetAgeBasedAllocation(attainedAge, riskPreference)` returns allocation rows for Blue Chip Equity Fund and Gilt Fund. [file:54]
- If no age-band mapping exists in master data, block illustration generation with a validation error instead of defaulting silently. [file:54]

## Validation

- If Investment Strategy is Age-based and Risk Preference is blank, highlight Risk Preference as mandatory. [file:54]
- If Investment Strategy is Self-Managed, Risk Preference must not be mandatory and should not be sent from UI. [file:54][file:55]
- If Self-Managed is selected, fund allocation total must equal 100 and every percentage must be a multiple of 5. [file:55]

## Backend DTO rule

Add these fields:
- `InvestmentStrategy`
- `RiskPreference`

And use these rules:
- `RiskPreference` nullable when strategy is Self-Managed. [file:54][file:55]
- `RiskPreference` required when strategy is Age-based. [file:54]

## Copilot implementation note

Do not derive risk preference behavior from UI conditions alone. Make it backend-driven from `ewealth_risk_preference_rules.csv` so screen logic, request validation, and calculation routing stay consistent. [code_file:62]
