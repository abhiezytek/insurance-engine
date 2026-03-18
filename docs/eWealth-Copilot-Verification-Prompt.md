# 🤖 GitHub Copilot Verification Prompt
## e-Wealth Royale (Unit-Linked) BI System

**Product:** SUD Life e-Wealth Royale (ULIF)  
**Document Date:** 2026-03-18  
**Purpose:** Complete GitHub Copilot instruction set for formula verification and code correctness

---

## COPILOT INSTRUCTION PROMPT
### Copy everything below and paste into GitHub Copilot chat:

```
SYSTEM: You are an expert insurance product calculation system for SUD Life e-Wealth Royale ULIF.
Your task is to verify and validate formulas, identify missing fields, correct calculation logic.

PRODUCT CONTEXT:
- Product: SUD Life e-Wealth Royale (Unit-Linked Insurance Fund)
- Policy Type: Endowment cum Investment-Linked
- Term Range: 10-20 years
- Premium Options: Yearly, Half-Yearly, Quarterly, Monthly
- Options: Platinum (Basic), Platinum Plus (With ARB)

=== SECTION 1: CRITICAL PARAMETERS (MUST VERIFY) ===

The following 17 parameters are CORE to all calculations:

PARAMETER LIST:
1. Age_LifeAssured (iAGE): Age of life assured | Cell: B11 | Sample: 37 | Used in: Mortality lookup, maturity age calc
2. Age_PolicyHolder (iPH_Age): Age of policy holder | Cell: B9 | Sample: 37 | Used in: ARB risk assessment
3. PolicyTerm (PT): Duration of policy | Cell: B19 | Sample: 20 | Used in: Years loop (1 to PT), maturity calculation
4. PremiumPaymentTerm (PPT): Years of premium payment | Cell: B21 | Sample: 10 | Used in: Premium logic (AP after year PPT = 0)
5. AnnualisedPremium (AP): Annual premium amount | Cell: B27 | Sample: 24000 | Used in: Fund calculation every year
6. SumAssured (SA): Life cover amount | Cell: B31 | Sample: 240000 | Used in: Death benefit = MAX(SA, Fund)
7. PremiumFrequency (Freq): Payment frequency | Cell: B25 | Sample: "Yearly" | Used in: Modal factor lookup
8. Option (OptCode): 1=Platinum, 2=Platinum Plus | Cell: B3 | Sample: 1 | Used in: ARB inclusion logic
9. MaturityAge (MA): Calculated as Age_LA + PT | Formula: B11+B19 | Sample: 57 | Used in: Year loop validation
10. PolicyAdminCharge (PAC_Annual): Fixed annual charge | Cell: Constant 1200 | Sample: 1200 | Used in: Fund deduction until PPT
11. FMC_Rate: Fund Management Charge | Cell: Constant 0.001118 (0.1118%) | Sample: 0.001118 | Used in: Fund_EoY calculation
12. InterestRate_4Pct: Scenario 1 return assumption | Cell: Constant 0.04 | Sample: 4% | Used in: Fund growth scenario 1
13. InterestRate_8Pct: Scenario 2 return assumption | Cell: Constant 0.08 | Sample: 8% | Used in: Fund growth scenario 2
14-17. ModalFactors (Lookup from table):
    - Yearly: 1.0000
    - HalfYearly: 0.5108
    - Quarterly: 0.2582
    - Monthly: 0.0867

VERIFICATION RULES:
✓ ALL parameters must be present in code/calculation
✓ Parameters must use EXACT cell references for inputs
✓ Calculated parameters must use formula (Age+Term, not hardcoded)
✓ Modal factors must be table-driven, not hardcoded
✓ Interest rates must support BOTH 4% and 8% scenarios separately

=== SECTION 2: INPUT FIELDS (31 TOTAL - VERIFY COMPLETENESS) ===

POLICY DETAILS (9 fields):
□ Option (B3): "Platinum" or "Platinum Plus" - CRITICAL for ARB logic
□ DOB_PolicyHolder (B5): Date format - Used for age calc verification
□ DOB_LifeAssured (B7): Date format - Used for mortality lookup
□ Age_PolicyHolder (B9): Number - CRITICAL for ARB assessment
□ Age_LifeAssured (B11): Number - CRITICAL for mortality charges
□ Gender_PolicyHolder (B13): "Male"/"Female" - ARB assessment factor
□ Gender_LifeAssured (B15): "Male"/"Female" - Mortality table lookup
□ Type_PPT (B17): "Limited" or "Till_Maturity"
□ PolicyTerm (B19): Number 10-20 - CRITICAL for year loop

PREMIUM DETAILS (5 fields):
□ PremiumPaymentTerm (B21): Number - CRITICAL logic gate (AP becomes 0 after PPT)
□ MaturityAge (B23): Should be FORMULA =B11+B19 (not manual entry)
□ PremiumFrequency (B25): "Yearly"/"HalfYearly"/"Quarterly"/"Monthly" - CRITICAL for modal factor
□ AnnualisedPremium (B27): Number - CRITICAL for fund deduction
□ PremiumInstallment (B29): = AP × ModalFactor or = AP × Frequency_Factor

DISTRIBUTION & RISK (6 fields):
□ DistributionChannel (B33): Text - For reporting
□ Staff_Family (B35): "YES"/"NO" - For discount logic (if applicable)
□ AgeRiskCommencement (B37): Number - Risk commencement age
□ EMR_Class_LifeAssured (B55): "Standard"/"Special" - For extra charges
□ EMR_Class_Policyholder (B57): "Standard"/"Special" - For extra charges
□ FlatExtra_LifeAssured (B63): Number - Extra premium per 1000 SA

INVESTMENT & FUND (5 fields):
□ FundOption (B39): "Self-Managed"/"Target_Maturity"/"Structured" - Reporting only
□ RiskPreference (B41): "Conservative"/"Moderate"/"Aggressive" - Reporting only
□ FundAllocation (B43:B54): Range of 12 fund options with % - Reporting & summary
□ GST_Rate (Constant): 0% for UL products in India - CRITICAL (not charged)
□ NetYield_4Pct (Calc_4): DERIVED from fund growth - Output calculation
□ NetYield_8Pct (Calc_8): DERIVED from fund growth - Output calculation

AGE PROOF & OTHER (6 fields):
□ AgeProof_LifeAssured (B59): "Yes"/"No" - Verification flag
□ AgeProof_Policyholder (B61): "Yes"/"No" - Verification flag
□ FlatExtra_Policyholder (B65): Number - Extra premium if any
□ KeralaFloodCess (B67): "Yes"/"No" - Tax calculation if applicable

STATUS: Verify ALL 31 fields present in code before fund calculation begins

=== SECTION 3: OUTPUT FIELDS (32 FIELDS IN 2 PARTS) ===

PART A: COMPARATIVE 4% vs 8% SCENARIOS (17 columns × 20 rows)

Column A: Policy_Year (1, 2, 3, ..., 20)
  → No formula needed, increment counter
  → Must match PolicyTerm range (1 to PT)

Columns B-I: 4% NET YIELD SCENARIO
Column B: Annualized_Premium 
  → Formula: =IF(Year <= PPT, AP, 0)
  → Year 1-10: 24000, Year 11-20: 0
  → CRITICAL: Premium stops after PPT

Column C: Mortality_Charges_4Pct
  → Formula: =VLOOKUP(Age+Year-1, MortalityTable, ReturnColumn_4Pct)
  → Year 1: 357 | Year 10: 338 | Year 20: 341
  → Status: ⚠️ VERIFY mortality table present & correct age progression

Column D: ARB_Charges_4Pct (if Option=2)
  → Formula: =IF(Option=2, ARB_Charge_Rate, 0)
  → Option 1 (Platinum): Always 0
  → Option 2 (Platinum Plus): Calculate from benefit
  → Sample Year 1: 0 (standard), Could be 0-500 depending on ARB

Column E: Other_Charges_4Pct
  → Formula: =PAC + Admin_Charges + GST_Charges
  → = 0 + 1200 + 0 = 1200 (for ULIF, GST=0)
  → Year 1-10: 1200, Year 11-20: 0

Column F: GST_4Pct
  → Formula: =0 (ULIF products in India have 0% GST)
  → Status: ⚠️ VERIFY - Should always be 0

Column G: Fund_EoY_4Pct (CRITICAL)
  → Formula: =(Fund_BoY + AP - TotalCharges) × (1+InterestRate) - FMC
  → OR: =Fund_BoY + AP - Mort_Ch - Admin_Ch + Investment_Income - FMC
  → Year 1: 23048 | Year 10: 281519
  → Status: ⚠️ CRITICAL - Verify compounding logic, FMC deduction timing

Column H: Surrender_Value_4Pct
  → Formula: =IF(Year >= 5, Fund_EoY, 0) with possible reduction
  → Year 1-4: 0 | Year 5+: Fund_Value
  → Status: ⚠️ VERIFY - Check for any surrender charge reduction

Column I: Death_Benefit_4Pct (CRITICAL)
  → Formula: =MAX(Sum_Assured, Fund_EoY)
  → Year 1-9 (at 4%): 240000 (SA limit) | Year 10+: Fund_Value
  → Status: ⚠️ CRITICAL - Verify MAX logic, not simple addition

Columns J-P: 8% NET YIELD SCENARIO
(Same formulas as Columns B-I but using InterestRate_8Pct = 8%)

Column J: Annualized_Premium (duplicate of Column B)
Column K: Mortality_Charges_8Pct (lookup with 8% column)
Column L: ARB_Charges_8Pct (if Option=2)
Column M: Other_Charges_8Pct (= 0 + 1200 + 0 = 1200)
Column N: GST_8Pct (= 0)
Column O: Fund_EoY_8Pct (CRITICAL - different from 4%)
Column P: Surrender_Value_8Pct (= IF Year>=5...)
Column Q: Death_Benefit_8Pct (= MAX(SA, Fund_EoY_8Pct))

Expected Sample Values (4% Scenario, Year 1):
  AP: 24000 | Mortality: 357 | ARB: 0 | Charges: 1200 | GST: 0 | Fund_EoY: 23048 | Surrender: 0 | Death_Benefit: 240000
  ✓ Verify all values present and correct

Expected Sample Values (8% Scenario, Year 1):
  AP: 24000 | Mortality: 356 | ARB: 0 | Charges: 1200 | GST: 0 | Fund_EoY: 23963 | Surrender: 0 | Death_Benefit: 240000
  ✓ Verify all values present and correct

---

PART B: DETAILED YEAR 1 CALCULATION BREAKDOWN (32 columns × 1 row for reporting)

This section shows FULL calculation detail for Year 1 at both rates:

Column A: Annualised_Premium
  → Value: 24000 (same as AP input)
  → Formula: =B27 (direct reference to input)

Column B: Premium_Allocation_Charges
  → Value: 0 (ULIF does not charge PAC, premium goes directly to fund)
  → Formula: =0 or =IF(Option=PremiumAllocationProduct, Calc, 0)
  → Status: ⚠️ VERIFY - Is PAC applicable? (Usually 0 for ULIF)

Column C: AP_minus_PAC
  → Formula: =AP - PAC = 24000 - 0 = 24000
  → This is amount going to fund

Column D: Mortality_Charges (calculated from lookup)
  → Year 1, Age 37: 357 (at 4%)
  → Formula: =VLOOKUP(Current_Age, MortalityTable, Rate_4_Column)
  → Status: ⚠️ VERIFY Mortality Table exists with age progression

Column E: ARB_Charges
  → Option 1 (Platinum): 0
  → Option 2 (Platinum Plus): Varies (could be 0-500 in Year 1)
  → Formula: =IF(Option=2, ARB_Rate × SA/1000, 0)
  → Status: ⚠️ VERIFY - ARB logic and rate table

Column F: GST_on_Charges
  → Value: 0 (ULIF products)
  → Formula: =0
  → Status: ✓ Fixed - Always 0 for ULIF

Column G: Policy_Admin_Charges
  → Value: 1200/year (until PPT, then 0)
  → Formula: =IF(Year <= PPT, 1200, 0)
  → Year 1: 1200 | Year 11: 0
  → Status: ⚠️ VERIFY - Check if this is deducted from fund

Column H: Extra_Premium_Allocation (EMR, etc.)
  → Value: 0 (if no extra mortality rate/flat extra)
  → Formula: =FlatExtra_Class1 × 1000/1000 + FlatExtra_Class2, etc.
  → Status: ⚠️ VERIFY - Check if applicable

Column I: Fund_Before_FMC
  → Formula: =Fund_BoY + (AP - PAC) - Mort_Ch - ARB_Ch - Admin_Ch - Extra_Ch
  → Year 1 Sample: =0 + 24000 - 357 - 0 - 1200 - 0 = 22443
  → Status: ⚠️ CRITICAL - Verify all charges deducted in sequence

Column J: FMC_Calculation
  → Formula: =Fund_Before_FMC × 0.001118
  → Year 1 Sample: =23074 × 0.001118 ≈ 26
  → Status: ⚠️ VERIFY - FMC rate 0.1118% is correct

Column K: Loyalty_Addition (Years 6,7,8,15 specific)
  → Year 1: 0 | Year 6: varies | Year 7: varies | Year 8: varies | Year 15: varies
  → Formula: =IF(Year IN [6,7,8,15], LoyaltyAmount, 0)
  → Status: ⚠️ VERIFY - Loyalty schedule and amounts for both rates

Column L: Wealth_Booster (Years 10,15,20 specific)
  → Year 1: 0 | Year 10: varies | Year 15: varies | Year 20: varies
  → Formula: =IF(Year IN [10,15,20], BoosterAmount, 0)
  → Status: ⚠️ VERIFY - Booster schedule and amounts for both rates

Column M: Return_of_Charges (Maturity bonus, if applicable)
  → Maturity Year (PT): Varies | Other years: 0
  → Formula: =IF(Year=PT, ChargeRefund, 0)
  → Status: ⚠️ VERIFY - Is this feature applicable?

Column N: Fund_EoY (CRITICAL)
  → Formula: =Fund_Before_FMC - FMC + Loyalty + Booster + Return_of_Charges
  → Year 1 Sample: =23074 - 26 + 0 + 0 + 0 = 23048
  → Status: ⚠️ CRITICAL - Verify FMC timing and bonus additions

Column O: Surrender_Value_5Yr_Lock
  → Formula: =IF(Year >= 5, Fund_EoY, 0)
  → Year 1-4: 0 | Year 5: 121638 (sample)
  → Status: ⚠️ VERIFY - Check for any surrender reduction factor

Column P: Death_Benefit (CRITICAL)
  → Formula: =MAX(Sum_Assured, Fund_EoY)
  → Year 1: =MAX(240000, 23048) = 240000
  → Year 10+: =MAX(240000, 281519+) varies
  → Status: ⚠️ CRITICAL - Verify MAX logic (not addition)

Plus 15 more columns for detailed breakdown...

STATUS: Verify all 32 output columns present, formulas correct, values match samples

=== SECTION 4: CRITICAL FORMULAS TO VERIFY ===

FORMULA 1: Premium After Payment Term
Name: Premium_Annual_Logic
Current: =IF(Year > PPT, 0, AP)
Expected: =IF(Year <= PPT, AP, 0)
Sample: Year 1-10 = 24000, Year 11-20 = 0
Status: ⚠️ VERIFY - Logic gate for premium termination
Why Critical: Fund calculation depends on premium availability every year

FORMULA 2: Mortality Charges Lookup
Name: Mortality_Age_Lookup
Current: Must verify VLOOKUP or INDEX/MATCH exists
Expected: =VLOOKUP(Current_Age, MortalityTable_4Pct, Column#)
Sample Year 1 (Age 37): 357
Sample Year 10 (Age 46): 338
Sample Year 20 (Age 56): 341
Status: ⚠️ CRITICAL - Verify mortality table present, age progression correct
Why Critical: Wrong mortality charges cascade to fund calculation

FORMULA 3: Fund End-of-Year Calculation (MOST CRITICAL)
Name: Fund_EoY_Calculation
Structure: 
  Step 1: Fund_BoY = Previous year Fund_EoY
  Step 2: Add Premium = Fund_BoY + AP (if Year <= PPT, else Fund_BoY + 0)
  Step 3: Deduct Charges = Fund_BoY + AP - Mortality_Ch - ARB_Ch - Admin_Ch
  Step 4: Apply Interest = (Fund_After_Charges) × (1 + InterestRate)
  Step 5: Deduct FMC = Fund_After_Interest - (Fund_After_Interest × FMC_Rate)
  Step 6: Add Loyalty/Booster = Fund_After_FMC + Loyalty + Booster

Current Formula (to verify):
Expected at 4%: Year 1 = 23048, Year 5 = 121638, Year 10 = 281519
Expected at 8%: Year 1 = 23963, Year 5 = 142571, Year 10 = 356782
Status: ⚠️ CRITICAL - Verify compounding logic, FMC deduction point
Why Critical: This is PRIMARY calculation, all outputs depend on it

FORMULA 4: Admin Charges (Year Logic)
Name: Policy_Admin_Charges_Cutoff
Current: =IF(Year <= PPT, 1200, 0)
Expected: Year 1-10 = 1200, Year 11-20 = 0
Sample: 10-year PPT, so Year 11 onwards = 0
Status: ✓ VERIFY - Logic is straightforward
Why Critical: Affects fund deduction in later years

FORMULA 5: Surrender Value (5-Year Lock)
Name: Surrender_Activation_Rule
Current: =IF(Year >= 5, Fund_EoY, "Locked/0")
Expected: Year 1-4 = 0, Year 5+ = Fund_Value
Sample: Year 5 (at 4%) = 121638
Status: ⚠️ VERIFY - Check for surrender charge reduction (if any)
Why Critical: Surrender availability is policy feature

FORMULA 6: Death Benefit (MAX Logic)
Name: Death_Benefit_Calculation
Current: =MAX(Sum_Assured, Fund_EoY)
Expected: NOT =SA + Fund (common error)
Sample Year 1: =MAX(240000, 23048) = 240000
Sample Year 10: =MAX(240000, 281519) = 281519 (Fund exceeds SA)
Status: ⚠️ CRITICAL - Verify MAX used, not + (addition)
Why Critical: Common coding error to add instead of MAX

FORMULA 7: Modal Factor Lookup
Name: Modal_Factor_Selection
Current: Must verify lookup from table OR hardcoded values
Expected Values (table-driven is better):
  "Yearly" → 1.0000
  "HalfYearly" → 0.5108
  "Quarterly" → 0.2582
  "Monthly" → 0.0867
Status: ⚠️ VERIFY - Should be table lookup, not IF statements
Why Critical: Modal factors are fixed but lookup is cleaner coding

FORMULA 8: ARB Logic (Option-Based)
Name: ARB_Inclusion_Conditional
Current: =IF(Option=2, ARB_Charge, 0)
Expected: 
  Option 1 (Platinum): All ARB columns = 0
  Option 2 (Platinum Plus): ARB charges apply
Sample Option 1: ARB_Ch = 0, Death_Benefit = MAX(SA, Fund)
Sample Option 2: ARB_Ch = (varies), Death_Benefit = MAX(SA + ARB_Benefit, Fund)
Status: ⚠️ CRITICAL - Verify Option branching logic
Why Critical: Different option calculations must not mix

=== SECTION 5: PARAMETER FORMULAS (MUST CHECK IF CALCULATED) ===

Check these 17 parameters - which ones are FORMULAS vs INPUTS?

INPUTS (No Formula Needed):
✓ Age_LifeAssured (B11): Direct input, no formula
✓ Age_PolicyHolder (B9): Direct input, no formula
✓ PolicyTerm (B19): Direct input, no formula
✓ PremiumPaymentTerm (B21): Direct input, no formula
✓ AnnualisedPremium (B27): Direct input, no formula
✓ SumAssured (B31): Direct input, no formula
✓ PremiumFrequency (B25): Direct input, no formula

CALCULATED (MUST HAVE FORMULAS):
⚠️ MaturityAge (B23): FORMULA REQUIRED = B11 + B19 (NOT manual entry)
⚠️ PremiumInstallment (B29): FORMULA REQUIRED = B27 × ModalFactor (OR = B27 × Frequency_Adjustment)
⚠️ Current_Age_In_Year_N: FORMULA = B11 + (Year - 1)
⚠️ YearsInForce: FORMULA = Year (from loop counter)

CONSTANTS (No Formula):
✓ PolicyAdminCharge = 1200 (constant)
✓ FMC_Rate = 0.001118 (constant)
✓ InterestRate_4Pct = 0.04 (constant)
✓ InterestRate_8Pct = 0.08 (constant)
✓ Modal Factors (lookup table values, not formulas)

STATUS: Verify MaturityAge and PremiumInstallment MUST use formulas, not manual values

=== SECTION 6: MISSING FIELDS CHECK ===

Common missing fields in ULIF calculations (verify presence):

□ MISSING: Loyalty_Addition column - Check if Year 6,7,8,15 logic exists
  Sample: Year 6 at 8% = 166, Year 7 = 200
  Status: ⚠️ VERIFY PRESENCE

□ MISSING: Wealth_Booster column - Check if Year 10,15,20 logic exists
  Sample: Year 10 at 8% = 8928, Year 15 = 13531, Year 20 = 19116
  Status: ⚠️ VERIFY PRESENCE

□ MISSING: Return_of_Charges column - Check maturity refund logic
  Sample: Year 20 (Maturity) = Refund amount or 0
  Status: ⚠️ VERIFY PRESENCE

□ MISSING: Policy_Admin_Charges_cutoff - Must verify PPT logic
  Status: ⚠️ VERIFY (Year > PPT = 0)

□ MISSING: FMC_Calculation column - Must be explicit
  Formula: =Fund_Before_FMC × 0.001118
  Status: ⚠️ VERIFY PRESENCE

□ MISSING: Fund_Before_FMC - Intermediate calculation critical
  Formula: =Fund_BoY + AP - All_Charges
  Status: ⚠️ VERIFY PRESENCE

□ MISSING: Surrender_Value column - For 5-year lock logic
  Status: ⚠️ VERIFY PRESENCE

=== SECTION 7: CONDITIONAL LOGIC REQUIREMENTS ===

These are the decision points in code - verify all present:

CONDITION 1: Premium After PPT
  Logic: IF (Year > PremiumPaymentTerm) THEN Deduct Premium = 0 ELSE Deduct Premium = AP
  Verification: Year 1-10 should show AP, Year 11-20 should show 0
  Sample Data: At Year 11, Annualized_Premium column = 0 ✓

CONDITION 2: Admin Charges Cutoff
  Logic: IF (Year <= PremiumPaymentTerm) THEN Admin_Ch = 1200 ELSE Admin_Ch = 0
  Verification: Year 1-10 = 1200, Year 11-20 = 0
  Sample Data: Year 11 Other_Charges should not include 1200 ✓

CONDITION 3: Option-Based ARB Logic
  Logic: IF (Option = "Platinum Plus") THEN Include ARB ELSE ARB = 0
  Verification: Platinum (Option 1) = no ARB columns, Platinum Plus (Option 2) = ARB columns populated
  Sample Data: Option 1 → ARB column = 0; Option 2 → ARB column = varies ✓

CONDITION 4: Surrender Value Activation
  Logic: IF (Year >= 5) THEN Surrender_Value = Fund_EoY ELSE Surrender_Value = 0 (or "Locked")
  Verification: Year 1-4 = 0, Year 5+ = Fund value
  Sample Data: Year 5 Surrender = 121638 ✓

CONDITION 5: Death Benefit MAX Logic
  Logic: Death_Benefit = MAX(Sum_Assured, Fund_EoY) [NOT addition]
  Verification: Year 1-9 = 240000 (SA), Year 10+ = Fund (exceeds SA)
  Sample Data: Year 1 Death = 240000, Year 10 Death = 281519 ✓

CONDITION 6: Mortality Charges Table Lookup
  Logic: Use VLOOKUP or INDEX/MATCH on MortalityTable with Current_Age
  Verification: Age 37 in Year 1 = 357 (4%), 356 (8%)
  Sample Data: Both rates lookup in separate mortality columns ✓

CONDITION 7: Loyalty Addition (Year-Based)
  Logic: IF (Year IN [6, 7, 8, 15]) THEN Add_Loyalty_Amount ELSE 0
  Verification: Year 6,7,8,15 = values; All other years = 0
  Sample Data: Year 6 (8%) = 166, Year 7 (8%) = 200, Year 8 (8%) = varies ✓

CONDITION 8: Wealth Booster (Year-Based)
  Logic: IF (Year IN [10, 15, 20]) THEN Add_Booster_Amount ELSE 0
  Verification: Year 10,15,20 = values; All other years = 0
  Sample Data: Year 10 (8%) = 8928, Year 15 (8%) = 13531, Year 20 (8%) = 19116 ✓

CONDITION 9: Return of Charges (Maturity Only)
  Logic: IF (Year = PolicyTerm) THEN Return_Charges ELSE 0
  Verification: Only maturity year should have non-zero value
  Sample Data: Year 20 = varies (or 0 if not applicable) ✓

CONDITION 10: Maturity Age Validation
  Logic: IF (Current_Age + PolicyTerm = MaturityAge) THEN Valid ELSE Flag
  Verification: Age 37 + PT 20 = MA 57
  Sample Data: B11 + B19 = B23 ✓

CONDITION 11: Fund Beginning of Year
  Logic: IF (Year = 1) THEN Fund_BoY = 0 ELSE Fund_BoY = Previous_Fund_EoY
  Verification: Year 1 Fund_BoY = 0, Year 2 Fund_BoY = Year 1 Fund_EoY
  Sample Data: Year 1 (4%) starting fund = 0, Year 5 = 109,515 from Year 4 ✓

=== SECTION 8: EXACT VERIFICATION VALUES (TEST AGAINST THESE) ===

Use these sample calculation values to verify code correctness:

SCENARIO 1: 4% NET YIELD
Policy: Platinum (Option 1), 20-year term, 10-year premium payment
Age: 37, Premium: 24000/year, SA: 240000

Year 1 Expected Values:
  - Annualized Premium: 24000
  - Mortality Charges: 357
  - ARB Charges: 0 (Platinum)
  - Admin Charges: 1200
  - GST: 0
  - Fund EoY: 23048
  - Surrender Value: 0 (Year 1, 5-year lock)
  - Death Benefit: 240000 (MAX(SA, Fund))

Year 5 Expected Values:
  - Annualized Premium: 24000
  - Mortality Charges: 340
  - Fund EoY: 121638
  - Surrender Value: 121638 (Year 5, lock released)
  - Death Benefit: 240000 (SA still higher)

Year 10 Expected Values:
  - Annualized Premium: 24000 (last premium year)
  - Mortality Charges: 338
  - Admin Charges: 1200 (last year)
  - Fund EoY: 281519
  - Surrender Value: 281519
  - Death Benefit: 281519 (Fund exceeds SA)

Year 11 Expected Values:
  - Annualized Premium: 0 (PPT ended)
  - Mortality Charges: 336
  - Admin Charges: 0 (PPT ended)
  - Fund EoY: 293387 (grows without premium/admin deduction)
  - Surrender Value: 293387
  - Death Benefit: 293387

Year 20 Expected Values (Maturity):
  - Annualized Premium: 0
  - Fund EoY: 412356 (approx, sample)
  - Surrender Value: 412356 (full fund available)
  - Death Benefit: 412356
  - Maturity Benefit: 412356

SCENARIO 2: 8% NET YIELD
(Same policy structure, but with 8% interest rate)

Year 1 Expected Values:
  - Annualized Premium: 24000
  - Mortality Charges: 356
  - Fund EoY: 23963
  - Surrender Value: 0
  - Death Benefit: 240000

Year 5 Expected Values:
  - Fund EoY: 142571
  - Surrender Value: 142571
  - Death Benefit: 240000

Year 10 Expected Values:
  - Fund EoY: 356782
  - Surrender Value: 356782
  - Death Benefit: 356782

Year 20 Expected Values (Maturity):
  - Fund EoY: 576283 (approx, sample with 8% returns)
  - Maturity Benefit: 576283

DIFFERENCE ANALYSIS:
- 4% vs 8% difference in 20 years: ~163,927 (8% significantly higher)
- Both scenarios must be calculated separately
- Both must produce independently correct values

=== SECTION 9: ABBREVIATIONS QUICK REFERENCE ===

AP = Annualized Premium (24000)
PPT = Premium Payment Term (10 years)
PT = Policy Term (20 years)
SA = Sum Assured (240000)
PAC = Premium Allocation Charges (typically 0 for ULIF)
ARB = Additional Risk Benefit (Platinum Plus only)
EMR = Extra Mortality Rate (if medical underwriting flags)
FMC = Fund Management Charge (0.1118% of fund)
GST = Goods and Services Tax (0% for ULIF in India)
ULIF = Unit-Linked Insurance Fund
UoW = Underwriting (assessment for EMR/ARB)
OptCode = Option Code (1=Platinum, 2=Platinum Plus)
Freq = Premium Frequency (Yearly/HY/Q/Monthly)
MA = Maturity Age (calculated)
DB = Death Benefit (MAX of SA and Fund)
SV = Surrender Value (available after Year 5)
MF = Modal Factor (1.0, 0.5108, 0.2582, 0.0867)

=== SECTION 10: QUICK COPILOT COMMANDS ===

Use these commands to interact with me for verification:

"Verify Fund_EoY formula for 4% scenario"
"Check Mortality_Charges lookup for age progression"
"Validate ARB logic for Platinum Plus option"
"Confirm Surrender_Value 5-year lock implementation"
"Test Death_Benefit MAX calculation against sample values"
"Verify Premium cutoff after Year 10 (PPT=10)"
"Check Admin_Charges Year 11+ = 0"
"Validate FMC deduction sequence in fund calc"
"Check Loyalty_Addition Year 6,7,8,15 logic"
"Verify Wealth_Booster Year 10,15,20 logic"
"List all missing input fields"
"List all missing output columns"
"Highlight formulas vs constants vs inputs"
"Compare current values against sample data"
"Identify calculation errors in fund growth"
"Confirm both 4% and 8% scenarios independent"

=== SECTION 11: FINAL VERIFICATION CHECKLIST ===

Before code is approved, verify EVERY item:

INPUT VERIFICATION:
□ All 31 input fields present
□ Option field correctly branches logic (Platinum vs Plus)
□ PolicyTerm used in year loop (1 to PT)
□ PremiumPaymentTerm used in premium cutoff (Year > PPT = 0)
□ MaturityAge calculated as formula (Age + PT)
□ PremiumInstallment calculated as formula (AP × ModalFactor)

OUTPUT VERIFICATION:
□ 17 columns for 4% scenario (A through I)
□ 17 columns for 8% scenario (J through P)
□ All 20 years populated (Year 1 through PT=20)
□ Premium stops after Year 10 (PPT=10)
□ Admin charges stop after Year 10

FORMULA VERIFICATION:
□ Mortality charges lookup correct (357 Year 1, age 37)
□ Fund_EoY calculation matches sample values (4%: 23048; 8%: 23963)
□ FMC deduction present and correct (≈26 on 23074)
□ Premium logic: Year 1-10 = AP, Year 11-20 = 0
□ Admin charges: Year 1-10 = 1200, Year 11-20 = 0
□ Surrender value: Year 1-4 = 0, Year 5+ = Fund
□ Death benefit: MAX(SA, Fund), not SA+Fund

OPTION VERIFICATION:
□ Platinum (Option 1): ARB columns = 0
□ Platinum Plus (Option 2): ARB columns populated

CONDITIONAL LOGIC:
□ IF statements for PPT cutoff present
□ IF statements for Admin charges cutoff present
□ IF statements for Surrender activation present
□ VLOOKUP or INDEX/MATCH for Mortality table present
□ MAX function for Death benefit (not +)

MISSING COLUMNS CHECK:
□ Fund_Before_FMC column present
□ FMC_Calculation column present
□ Loyalty_Addition column present (Years 6,7,8,15)
□ Wealth_Booster column present (Years 10,15,20)
□ Surrender_Value column present (5-yr lock logic)

CONSTANTS CHECK:
□ Policy Admin Charge = 1200/year
□ FMC Rate = 0.001118 (0.1118%)
□ Interest Rate 4% = 0.04
□ Interest Rate 8% = 0.08
□ GST = 0% (not charged on ULIF)
□ Modal Factors: Yearly=1.0, HY=0.5108, Q=0.2582, M=0.0867

SAMPLE VALUE VERIFICATION:
□ Year 1 Fund_EoY (4%): 23048 ✓
□ Year 1 Fund_EoY (8%): 23963 ✓
□ Year 5 Fund_EoY (4%): 121638 ✓
□ Year 10 Fund_EoY (4%): 281519 ✓
□ Year 10 Death_Benefit (4%): 281519 ✓
□ Year 1 Mortality (Age 37): 357 (4%) / 356 (8%) ✓

CALCULATION LOGIC:
□ Year 1 has Fund_BoY = 0
□ Year 2 has Fund_BoY = Year 1 Fund_EoY
□ Compounding occurs correctly each year
□ FMC deduction happens after all charges
□ Bonuses (Loyalty/Booster) added after FMC

=== SECTION 12: KNOWN ISSUES TO AVOID ===

Common errors in ULIF calculations (verify code is NOT doing these):

❌ ERROR: Death Benefit = SA + Fund (should be MAX)
   Example: Year 10 = 240000 + 281519 = 521519 (WRONG)
   Correct: MAX(240000, 281519) = 281519

❌ ERROR: Premium continues after PPT
   Example: Year 11-20 still show AP = 24000 (WRONG)
   Correct: Year 11-20 = 0

❌ ERROR: Admin Charges continue after PPT
   Example: Year 11-20 still deduct 1200 (WRONG)
   Correct: Year 11-20 = 0

❌ ERROR: Surrender Value available Year 1
   Example: Year 1 Surrender = 23048 (WRONG)
   Correct: Year 1-4 = 0, Year 5+ = Fund

❌ ERROR: Mortality charges not age-progressed
   Example: All years show 357 (WRONG)
   Correct: Year 1=357, Year 2=356, Year 3=354, etc.

❌ ERROR: FMC not deducted from fund
   Example: Fund_EoY shows 23048 without FMC deduction (WRONG)
   Correct: Fund shows FMC deducted (≈26)

❌ ERROR: Both scenarios (4% & 8%) using same fund value
   Example: Column O (8%) = Column G (4%) (WRONG)
   Correct: Column O calculated separately with 8% rate

❌ ERROR: ARB columns present when Option=1 (Platinum)
   Example: All option 1 policies show ARB charges (WRONG)
   Correct: Option 1 → ARB columns = 0

❌ ERROR: Maturity Age hardcoded instead of formula
   Example: B23 = 57 (hardcoded) (NOT IDEAL)
   Correct: B23 = =B11+B19 (formula)

❌ ERROR: Modal Factor hardcoded instead of looked up
   Example: All years use 1.0 regardless of frequency (WRONG if HY/Q/M)
   Correct: Lookup from Modal Factor table based on Frequency

ERROR REMEDIATION:
If you find ANY of the above errors:
1. Flag the specific formula/cell
2. Show the CURRENT (incorrect) value
3. Show the EXPECTED (correct) value
4. Provide corrected formula
5. Re-test against sample data

---

END OF COPILOT INSTRUCTION PROMPT
```

---

## 📋 HOW TO USE THIS PROMPT WITH GITHUB COPILOT

### Step 1: Prepare Your Code
- Have GitHub Copilot open in your IDE
- Have the current code (Excel formulas or implementation code) available

### Step 2: Copy the Prompt
- Copy everything between the "COPILOT INSTRUCTION PROMPT" markers above
- The prompt is self-contained and comprehensive

### Step 3: Start Verification Chat
- Paste the entire prompt into a new Copilot conversation
- Copilot will load all context about parameters, formulas, and requirements

### Step 4: Use Quick Commands
- After prompt loads, use any of the commands from SECTION 10
- Examples:
  - "Verify Fund_EoY formula for 4% scenario"
  - "Check if all 31 input fields are present"
  - "Identify any missing output columns"
  - "Compare current values against sample data"

### Step 5: Address Issues
- Copilot will identify:
  - Missing input/output fields
  - Incorrect formula logic
  - Wrong cell references
  - Calculation errors
  - Conditional logic issues

### Step 6: Implement Corrections
- Copilot can help write corrected formulas
- Test against provided sample values
- Iterate until all verification checks pass

---

## 🎯 VERIFICATION PRIORITY ORDER

### HIGHEST PRIORITY (Fix First):
1. **Fund_EoY calculation** - This is the core, everything depends on it
2. **Mortality charges lookup** - Affects fund deduction every year
3. **Death benefit MAX logic** - Common error (addition vs MAX)
4. **Premium cutoff after PPT** - Must become 0 after Year 10
5. **FMC deduction** - Embedded in fund calculation

### HIGH PRIORITY (Fix Second):
6. Admin charges cutoff after PPT
7. Surrender value 5-year lock activation
8. ARB option branching (Platinum vs Plus)
9. Fund_Before_FMC intermediate calculation
10. Loyalty and Wealth Booster logic

### MEDIUM PRIORITY (Verify After):
11. All input field collection
12. All output column presence
13. Parameter formula status (MaturityAge, etc.)
14. Modal factor lookup
15. Sample value accuracy

### VERIFICATION COMPLETION:
- ✅ Run full checklist from Section 11
- ✅ Test against all sample values in Section 8
- ✅ Verify both 4% and 8% scenarios independently
- ✅ Confirm no errors from Section 12 (Known Issues)
- ✅ Code ready for implementation

---

## 📊 REFERENCE CSV FILES (Use Alongside This Prompt)

Three CSV files have been created to support quick lookups:

1. **eWealth-Input-Fields-Reference.csv** - All 31 input fields with cell references
2. **eWealth-Output-Fields-Reference.csv** - All 32 output fields with sample values
3. **eWealth-Formulas-Reference.csv** - 12 critical formulas with expected values
4. **eWealth-Parameters-Reference.csv** - 17 core parameters and their usage
5. **eWealth-Abbreviations-Reference.csv** - 10 key abbreviations used
6. **eWealth-Options-Comparison.csv** - Platinum vs Platinum Plus comparison

Use these CSVs to:
- Cross-check field names and cell references
- Look up formula details quickly
- Verify sample calculation values
- Reference abbreviations while working

---

## ✅ SUCCESS CRITERIA

Your code is ready when:

- [x] All 31 input fields collected and validated
- [x] All 32 output fields calculated correctly
- [x] Fund_EoY matches sample values (4%: 23048 Yr1, 281519 Yr10; 8%: 23963 Yr1, 356782 Yr10)
- [x] Premium stops after Year 10 (PPT=10)
- [x] Admin charges stop after Year 10
- [x] Surrender value starts at Year 5 (5-year lock)
- [x] Death benefit uses MAX(SA, Fund), not addition
- [x] Mortality charges progress with age (357→356→354...)
- [x] FMC deducted from fund (~26 on year 1)
- [x] ARB logic branches on Option field
- [x] Both 4% and 8% scenarios independent
- [x] All verification checklist items checked ✓

---

**Document Generated:** 2026-03-18  
**Product:** SUD Life e-Wealth Royale (ULIF)  
**Version:** 1.0  
**Ready for:** GitHub Copilot Verification  

**Status:** ✅ COMPLETE & READY FOR COPILOT VERIFICATION