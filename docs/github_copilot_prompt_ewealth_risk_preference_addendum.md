# Addendum for GitHub Copilot — Risk Preference handling

Use these additional files:
- ewealth_risk_preference_rules.csv
- ewealth_risk_preference_rules.md

Apply these corrections:
1. Keep Risk Preference in the module, but only for Age-based Investment Strategy.
2. Show Risk Preference dropdown values `Aggressive` and `Conservative` only when Investment Strategy = Age-based Investment Strategy.
3. Hide or disable Risk Preference when Investment Strategy = Self-Managed Investment Strategy.
4. Route calculation logic by strategy:
   - Age-based -> use backend age-based allocation master for Blue Chip Equity Fund and Gilt Fund.
   - Self-managed -> use user-entered fund allocation.
5. Make validation backend-driven using the CSV rules.
6. Do not hardcode UI-only logic for this field.
7. If age-based allocation master is missing, raise a validation/configuration error rather than silently calculating wrong allocations.
