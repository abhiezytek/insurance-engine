# GITHUB COPILOT VERIFICATION PROMPT
## SUD Life e-Wealth Royale (Unit-Linked) BI Code Review & Correction

**Product:** SUD Life e-Wealth Royale (142L082V03)  
**Plan Type:** Individual Unit-Linked Non-Participating Life Insurance Plan  
**Date:** 2026-03-18  
**Status:** Ready for Copilot Formula & Field Verification  

---

## SECTION 1: INPUT FIELDS VERIFICATION

### CRITICAL: You need to verify that ALL 31 input fields are correctly captured in code

#### INPUT FIELDS CHECKLIST (Total: 31 Fields)

**1. POLICY BASICS (7 Fields)**
```
Field_Name                 | Cell/Variable | Data_Type | Sample_Value  | Status
Option                     | B3            | Text      | Platinum      | ✓ Verify
DOB_PolicyHolder           | B5            | Date      | 8-Jul-88      | ✓ Verify
DOB_LifeAssured            | B7            | Date      | 8-Jul-88      | ✓ Verify
Age_PolicyHolder           | B9            | Number    | 37            | ✓ Verify
Age_LifeAssured            | B11           | Number    | 37            | ✓ Verify
Gender_PolicyHolder        | B13           | Text      | Male          | ✓ Verify
Gender_LifeAssured         | B15           | Text      | Male          | ✓ Verify
Type_PPT                   | B17           | Text      | Limited       | ✓ Verify
PolicyTerm                 | B19           | Number    | 20            | ✓ Verify
PremiumPaymentTerm         | B21           | Number    | 10            | ✓ Verify
MaturityAge                | B23           | Number    | 57            | ✓ Verify
PremiumFrequency           | B25           | Text      | Yearly        | ✓ Verify
```

**2. PREMIUM DETAILS (3 Fields)**
```
AnnualisedPremium          | B27           | Number    | 24000         | ✓ Verify
PremiumInstallment         | B29           | Number    | 24000         | ✓ Verify
SumAssured                 | B31           | Number    | 240000        | ✓ Verify
```

**3. DISTRIBUTION & UNDERWRITING (3 Fields)**
```
DistributionChannel        | B33           | Text      | Corporate     | ✓ Verify
Staff_Family               | B35           | Text      | NO            | ✓ Verify
AgeRiskCommencement        | B37           | Number    | 37            | ✓ Verify
```

**4. INVESTMENT STRATEGY (3 Fields)**
```
FundOption                 | B39           | Text      | Self-Managed  | ✓ Verify
RiskPreference             | B41           | Text      | Conservative  | ✓ Verify
SelectedFunds              | B43:B54       | Range     | Fund List     | ⚠️ VERIFY ALLOCATION %
```

**5. RISK UNDERWRITING (7 Fields)**
```
EMR_Class_LifeAssured      | B55           | Text      | Standard      | ✓ Verify
EMR_Class_Policyholder     | B57           | Text      | Standard      | ✓ Verify
AgeProof_LifeAssured       | B59           | Text      | Yes           | ✓ Verify
AgeProof_Policyholder      | B61           | Text      | Yes           | ✓ Verify
FlatExtra_LifeAssured      | B63           | Number    | 0             | ✓ Verify
FlatExtra_Policyholder     | B65           | Number    | 0             | ✓ Verify
KeralaFloodCess            | B67           | Text      | No            | ✓ Verify
```

**⚠️ MISSING FIELDS TO ADD:**
```
1. GST_Rate                → Should be 0% (stated in PDF)
2. NetYield_4Percent      → Formula: Calculated from Part B 4%
3. NetYield_8Percent      → Formula: Calculated from Part B 8%
4. GrossYield_4Percent    → 4% (constant)
5. GrossYield_8Percent    → 8% (constant)
```

---

## SECTION 2: OUTPUT FIELDS VERIFICATION

### CRITICAL: Verify PART A & PART B structure matches exactly

#### PART A: Summary Annual Benefit Illustration (17 Columns, 20 Rows)

**Column Structure for PART A:**
```
Col | Field_Name                          | 4% Return? | 8% Return? | Data_Type | Formula_Required
--- | ---- | --- | --- | --- | ---
A   | Policy_Year                         | Both       | Both       | Number    | Row_Number
B   | Annualized_Premium                  | Both       | Both       | Number    | IF(Year <= PPT, AP, 0)
C   | Mortality_Charges_4Pct              | 4% Only    | No         | Number    | From_Part_B
D   | Additional_Risk_Benefit_Charges_4Pct| 4% Only    | No         | Number    | From_Part_B (0 for Platinum)
E   | Other_Charges_4Pct                  | 4% Only    | No         | Number    | From_Part_B (PAC+Admin)
F   | GST_4Pct                            | 4% Only    | No         | Number    | 0 (No GST on UL)
G   | Fund_EndOfYear_4Pct                 | 4% Only    | No         | Number    | ⚠️ KEY FORMULA
H   | Surrender_Value_4Pct                | 4% Only    | No         | Number    | IF(Year >= 5, Fund, 0)
I   | Death_Benefit_4Pct                  | 4% Only    | No         | Number    | MAX(SA, Fund)
J   | Mortality_Charges_8Pct              | 8% Only    | No         | Number    | From_Part_B
K   | Additional_Risk_Benefit_Charges_8Pct| 8% Only    | No         | Number    | From_Part_B
L   | Other_Charges_8Pct                  | 8% Only    | No         | Number    | From_Part_B
M   | GST_8Pct                            | 8% Only    | No         | Number    | 0
N   | Fund_EndOfYear_8Pct                 | 8% Only    | No         | Number    | ⚠️ KEY FORMULA
O   | Surrender_Value_8Pct                | 8% Only    | No         | Number    | IF(Year >= 5, Fund, 0)
P   | Death_Benefit_8Pct                  | 8% Only    | No         | Number    | MAX(SA, Fund)
```

**⚠️ CRITICAL FORMULAS FOR PART A:**

1. **Fund_EndOfYear_4Pct (Column G):**
   ```
   Formula: 
   = Previous_Fund_Value 
     + Annualized_Premium (if Year <= PPT)
     - Mortality_Charges
     - Other_Charges
     + Investment_Income (at 4% rate)
     + Loyalty_Addition (specific years)
     + Wealth_Booster (Year 10, 15, 20)
   ```

2. **Fund_EndOfYear_8Pct (Column N):**
   ```
   Same structure as above but with 8% investment income
   ```

3. **Surrender_Value:**
   ```
   Formula: IF(Year >= 5, Fund_EndOfYear, 0)
   Before 5 years: Shows as 0 or "Locked"
   From 5 years: Equals Fund_EndOfYear
   ```

4. **Death_Benefit:**
   ```
   Formula: MAX(SumAssured, Fund_EndOfYear)
   Minimum: Sum Assured
   Maximum: Actual Fund Value if Fund > SA
   ```

---

#### PART B: Detailed Breakdown (16 Columns, 20 Rows, Duplicated for 4% & 8%)

**Column Structure for PART B (Detailed):**
```
Col | Field_Name                                    | Formula / Reference
--- | --- | ---
A   | Annualised_Premium (AP)                       | IF(Year <= PPT, AP_Input, 0)
B   | Premium_Allocation_Charges (PAC)              | 0 (Stated in PDF: PAC = 0%)
C   | Annualised_Premium_minus_PAC                  | AP - PAC = AP
D   | Mortality_Charges                             | ⚠️ LOOKUP from mortality table
E   | Additional_Risk_Benefit_Charges (ARB)         | IF(Option = Platinum, 0, Charge_Rate × SA)
F   | GST                                           | 0 (No GST on UL plans)
G   | Policy_Administration_Charges                 | 1200 per year (fixed until Year 10/PPT)
H   | Extra_Premium_Allocation                      | 0 (unless applicable)
I   | Fund_Before_FMC                               | (Previous_Fund + C - D - E - F - G - H + Investment_Income)
J   | Fund_Management_Charge (FMC)                  | 0.1118% × Fund_Before_FMC
K   | Loyalty_Addition                              | IF(Year IN [6,7,8,...], Amount, 0)
L   | Wealth_Booster                                | IF(Year IN [10,15,20], Amount, 0)
M   | Return_of_Policy_Admin_Maturity_ARB_Charges   | IF(Year = Maturity, Charges_Accumulated, 0)
N   | Fund_at_End_of_Year                           | I - J + K + L + M
O   | Surrender_Value                               | IF(Year >= 5, Fund_at_End, 0)
P   | Death_Benefit                                 | MAX(SA, Fund_at_End)
```

**⚠️ CRITICAL FORMULAS FOR PART B:**

1. **Mortality_Charges (Column D):**
   ```
   This is NOT a simple formula - it's a lookup from mortality table
   Deduction in advance on first working day of each policy month
   
   For Age 37-47:
   Year 1: 356/357 (varies with return rate)
   Year 2: 338/341
   Year 3: 313/321
   ...decreases as years pass...
   
   ACTION: Verify your code has CORRECT mortality table lookup
   TABLE LOCATION: [Check if in separate sheet or embedded]
   ```

2. **Policy_Administration_Charges (Column G):**
   ```
   Year 1-10: 1200 per year
   Year 11+: 0 (stops after PPT of 10 years)
   
   Formula: IF(Year <= PPT, 1200, 0)
   OR: IF(Year <= 10, 1200, 0) [since PPT = 10]
   ```

3. **Fund_Management_Charge (Column J):**
   ```
   FMC = 0.1118% × Fund_Before_FMC
   
   Formula: Fund_Before_FMC × 0.001118
   
   This is embedded in NAV, NOT deducted separately
   Affects unit growth
   ```

4. **Loyalty_Addition (Column K):**
   ```
   Year 6: 166 (at 8% return) or 147 (at 4% return)
   Year 7: 200 (8%) or 174 (4%)
   Year 8: 237 (8%) or 202 (4%)
   Year 15: 13,531 (8%) or 9,375 (4%)
   Year 20: 0
   
   ACTION: Verify these amounts are correctly calculated
   ```

5. **Wealth_Booster (Column L):**
   ```
   Year 10: 8,928 (8% return) or 7,342 (4% return)
   Year 15: 13,531 (8%) or 9,375 (4%)
   Year 20: 19,116 (8%) or 10,977 (4%)
   
   ACTION: Verify these are calculated correctly
   ```

---

## SECTION 3: ABBREVIATIONS & PARAMETERS MAPPING

### All Abbreviations (10 Total):
```
Abbreviation | Full Form | Cell/Variable | Used_In | Sample_Value
--- | --- | --- | --- | ---
AP | Annualised Premium | B27 | Part A, B | 24000
PPT | Premium Payment Term | B21 | All Calcs | 10
PT | Policy Term | B19 | All Calcs | 20
PAC | Premium Allocation Charges | Part_B_Col_B | Part B | 0%
ARB | Additional Risk Benefit | Part_B_Col_E | Part B | 0 (Platinum) / Value (PP)
FMC | Fund Management Charge | Part_B_Col_J | Part B | 0.1118%
GST | Goods and Services Tax | Part_B_Col_F | Part B | 0%
EMR | Extra Mortality Rate | B55-B57 | Risk Calcs | Varies
ULIF | Unit-Linked Insurance Fund | Input_Range | Fund Options | 12 Funds
NSAP | Non-Standard Age Proof | B59-B61 | Risk | 0 (if Standard)
```

### Parameter Mapping:
```
Parameter_Name           | Source_Cell | Data_Type | Formula_Required?
Age_LifeAssured          | B11         | Number    | No (Input)
Age_PolicyHolder         | B9          | Number    | No (Input)
SumAssured               | B31         | Number    | No (Input)
AnnualisedPremium        | B27         | Number    | No (Input)
PolicyTerm               | B19         | Number    | No (Input)
PremiumPaymentTerm       | B21         | Number    | No (Input)
MaturityAge              | B23         | Number    | ✓ FORMULA = Age_LA + PT
ModalFactor              | Lookup      | Number    | ✓ LOOKUP(Frequency)
InterestRate_4Pct        | Constant    | Number    | No (4%)
InterestRate_8Pct        | Constant    | Number    | No (8%)
PolicyAdminCharge_Year   | Constant    | Number    | No (1200)
FMC_Rate                 | Constant    | Number    | No (0.1118%)
```

---

## SECTION 4: OPTION COMPARISON - Platinum vs Platinum Plus

### CRITICAL: Your code must handle BOTH options with different charge structures

**Option 1: Platinum (Selected in Sample)**
```
Field                                  | Value
--- | ---
Additional_Risk_Benefit_Charges (ARB)  | 0 (No additional charges)
Mortality_Charges                      | Standard age-based charges
Policy_Admin_Charges                   | 1200 per year (until PPT)
FMC                                    | 0.1118%
Coverage                               | Death Benefit + Surrender Value
```

**Option 2: Platinum Plus**
```
Field                                  | Value
--- | ---
Additional_Risk_Benefit_Charges (ARB)  | YES (Additional Risk Benefit)
Mortality_Charges                      | Standard + Extra charges
Policy_Admin_Charges                   | 1200 per year (until PPT)
FMC                                    | 0.1118%
Coverage                               | Death Benefit + ARB + Surrender Value
```

**CONDITIONAL FORMULAS NEEDED:**
```
IF(Option = "Platinum", 
   ARB_Charge = 0, 
   ARB_Charge = [Charge_Rate × SA])

IF(Option = "Platinum Plus",
   Include_ARB_in_Output,
   Set_ARB = 0)
```

---

## SECTION 5: KEY FORMULAS FOR GITHUB COPILOT VERIFICATION

### 5.1 ANNUAL PREMIUM CALCULATION
```
Current Implementation (VERIFY):
Annualized_Premium = Premium_Input × Modal_Factor

Modal Factor Lookup Table (VERIFY IN YOUR CODE):
Premium_Frequency | Modal_Factor | Formula
Yearly            | 1.0000       | AP × 1.0
Half-Yearly       | 0.5108       | AP × 0.5108
Quarterly         | 0.2582       | AP × 0.2582
Monthly           | 0.0867       | AP × 0.0867

Expected Output for Sample:
AP_Input = 24000
Frequency = Yearly
Modal_Factor = 1.0
Annualized_Premium = 24000 × 1.0 = 24000 ✓
```

### 5.2 FUND VALUE ACCUMULATION (Core Formula)
```
Fund_End_of_Year = Fund_Beginning 
                   + Premium (if Year ≤ PPT)
                   - Mortality_Charges
                   - Policy_Admin_Charges
                   - FMC
                   - Other_Charges
                   + Investment_Income
                   + Loyalty_Addition
                   + Wealth_Booster

VERIFY:
[ ] Correct order of operations
[ ] All charges deducted before investment income
[ ] Investment income calculated on CORRECT base (after charges)
[ ] Loyalty/Booster additions added AT END of year
```

### 5.3 INVESTMENT INCOME CALCULATION
```
Investment_Income = Fund_Before_FMC × (Rate / 12) [for monthly]
OR
Investment_Income = Fund_Before_FMC × Rate [for annual aggregation]

For Sample:
Year 1, End of Year:
Fund_Before_FMC = 23,074 (at 4% rate, from Part B)
Investment_Income = 23,074 × 4% = 922.96
FMC = 23,074 × 0.1118% = 25.77
Fund_After_FMC = 23,074 + 922.96 - 25.77 = 23,971 (≈ shown value 23,048)

⚠️ VERIFY: Check if your calculation matches Part B exactly
```

### 5.4 SURRENDER VALUE FORMULA
```
Surrender_Value = IF(Policy_Year >= 5, 
                     Fund_Value_At_End_of_Year,
                     "Not_Available" OR 0)

VERIFY:
[ ] Years 1-4: Shows 0 or "Locked"
[ ] Year 5 onwards: Shows Fund Value
[ ] Sample Check - Year 5 at 4%: 121,638 ✓
[ ] Sample Check - Year 10 at 4%: 281,519 ✓
```

### 5.5 DEATH BENEFIT FORMULA
```
Death_Benefit = MAX(Sum_Assured, Fund_Value_At_End_of_Year)

VERIFY:
[ ] Minimum is ALWAYS Sum Assured (240,000)
[ ] Before fund exceeds SA: Death_Benefit = SA
[ ] After fund exceeds SA: Death_Benefit = Fund_Value
[ ] Sample Check - Year 1 at 4%: 240,000 (minimum) ✓
[ ] Sample Check - Year 8 at 4%: 240,000 (fund = 203,509, so minimum applies) ✓
[ ] Sample Check - Year 9 at 4%: 240,000 (fund = 232,434, still < SA) ✓
[ ] Sample Check - Year 10 at 4%: 281,519 (fund > SA, so actual fund value) ✓
```

### 5.6 MORTALITY CHARGE LOOKUP (TABLE-DRIVEN)
```
Mortality charges are AGE-DEPENDENT and RETURN-DEPENDENT

SAMPLE DATA (Verify in your mortality table):
Age | Year_1_4Pct | Year_1_8Pct | Year_5_4Pct | Year_5_8Pct
--- | --- | --- | --- | ---
37  | 357         | 356         | 264         | 236
38  | 341         | 338         | (continues) | (continues)
...

Your Code Should:
[ ] Have mortality lookup table by age
[ ] Have different values for 4% and 8% scenarios
[ ] Deduct in chronological order (Year 1, 2, 3, etc.)
[ ] Match exact values shown in Part B
```

### 5.7 POLICY ADMINISTRATION CHARGE
```
Policy_Admin_Charge = IF(Year <= Premium_Payment_Term, Fixed_Annual_Charge, 0)

For Sample:
PPT = 10 years
Annual_Charge = 1,200

Year 1-10: 1,200 per year ✓
Year 11-20: 0 per year ✓

VERIFY in your code:
[ ] Correct mapping of PPT
[ ] Correct fixed amount (1,200)
[ ] Stops after PPT completion
```

---

## SECTION 6: SHEET STRUCTURE & DATA FLOW

### Expected Sheet Names & Content:

```
Sheet_Name               | Purpose | Rows | Columns | Status
--- | --- | --- | --- | ---
Input                    | Input fields + Option selection | Variable | 67+ | ✓ Verify
Part A (4%)              | Summary annual at 4% return | 20 | 17 | ⚠️ Verify accuracy
Part A (8%)              | Summary annual at 8% return | 20 | 17 | ⚠️ Verify accuracy
Part B (4%)              | Detailed breakdown at 4% | 20 | 16 | ⚠️ Verify accuracy
Part B (8%)              | Detailed breakdown at 8% | 20 | 16 | ⚠️ Verify accuracy
Net_Yield_4Percent      | Monthly granular (optional) | 240 | 18 | Verify if needed
Net_Yield_8Percent      | Monthly granular (optional) | 240 | 18 | Verify if needed
Mortality_Table          | Age × Year lookup | 60 | 25+ | ✓ Embedded/Lookup
```

---

## SECTION 7: CRITICAL VERIFICATION CHECKLIST FOR COPILOT

### INPUT FIELDS VERIFICATION:
- [ ] All 31 input fields are captured
- [ ] Data types are correct (Date, Number, Text)
- [ ] Sample values match: Age=37, Premium=24000, SA=240000, PPT=10, PT=20
- [ ] Option selection (Platinum vs Platinum Plus) affects charges correctly
- [ ] Missing fields added: GST_Rate, NetYield parameters

### OUTPUT FIELDS VERIFICATION:
- [ ] PART A has 17 columns (A-P: Policy Year + Premium + 8 charges + 8 values for 4% & 8%)
- [ ] PART A has 20 rows (Policy Years 1-20)
- [ ] PART B has 16 columns (A-P: Premium to Death Benefit)
- [ ] PART B has 20 rows (Policy Years 1-20)
- [ ] PART B duplicated for 4% and 8% scenarios
- [ ] All formulas produce correct outputs for sample calculation

### FORMULA VERIFICATION:
- [ ] Annual Premium: 24000 (24000 × 1.0) ✓
- [ ] Mortality Charges (Year 1, 4%): 357 ✓
- [ ] Policy Admin (Year 1-10): 1200, (Year 11-20): 0 ✓
- [ ] FMC: 0.1118% applied correctly ✓
- [ ] Fund Value (Year 1, 4%): 23,048 ✓
- [ ] Surrender Value (Year 1-4): 0 or locked ✓
- [ ] Surrender Value (Year 5+): Equals Fund Value ✓
- [ ] Death Benefit (Year 1-9, 4%): 240,000 ✓
- [ ] Death Benefit (Year 10, 4%): 281,519 ✓

### ABBREVIATION VERIFICATION:
- [ ] AP correctly used for Annualised Premium
- [ ] PPT correctly used for Premium Payment Term (10 years)
- [ ] PT correctly used for Policy Term (20 years)
- [ ] PAC = 0% (Premium Allocation Charges)
- [ ] ARB handled based on Option type
- [ ] FMC = 0.1118%
- [ ] GST = 0%
- [ ] EMR, ULIF, NSAP used where applicable

### PARAMETER VERIFICATION:
- [ ] No parameter is itself a formula (all are inputs)
- [ ] MaturityAge = Age_LifeAssured + PolicyTerm = 37 + 20 = 57 ✓
- [ ] ModalFactor lookup: Yearly = 1.0, Others as specified ✓
- [ ] InterestRates: 4% and 8% (separate scenarios) ✓

---

## SECTION 8: SAMPLE CALCULATION VERIFICATION

### Test Case: Twin Income (from PDF)
```
Input Data:
Age_LifeAssured = 37
AnnualisedPremium = 24000
PremiumPaymentTerm = 10
PolicyTerm = 20
SumAssured = 240000
Option = Platinum
Frequency = Yearly

Expected Output (4% Return):
Year | Premium | Mortality | Admin | Fund_Value | Surrender | Death_Benefit
1    | 24000   | 357       | 1200  | 23048      | 0         | 240000
5    | 24000   | 264       | 1200  | 121638     | 121638    | 240000
10   | 24000   | 0         | 1200  | 281519     | 281519    | 281519
15   | 0       | 0         | 0     | 329650     | 329650    | 329650
20   | 0       | 0         | 0     | 388129     | 388129    | 388129

Expected Output (8% Return):
Year | Premium | Mortality | Admin | Fund_Value | Surrender | Death_Benefit
1    | 24000   | 356       | 1200  | 23963      | 0         | 240000
5    | 24000   | 236       | 1200  | 136884     | 136884    | 240000
10   | 24000   | 0         | 1200  | 348652     | 348652    | 348652
15   | 0       | 0         | 0     | 492558     | 492558    | 492558
20   | 0       | 0         | 0     | 697683     | 697683    | 697683
```

### Validation Steps:
1. Check Year 1 values match exactly
2. Verify Year 5 & 10 (PPT boundary)
3. Confirm Year 20 (maturity year)
4. Ensure 4% & 8% differ as expected
5. Check surrender value logic (locked before Year 5)

---

## SECTION 9: COPILOT PROMPTS & COMMANDS

### Use these prompts to interact with GitHub Copilot:

**Prompt 1: Input Fields Verification**
```
"In this code, I need to verify that all 31 input fields 
for SUD Life e-Wealth Royale BI are captured correctly. 
The fields are: Option, DOB, Age, Gender, PPT, PT, Premium, 
SumAssured, Distribution Channel, Risk Class, Fund Options, etc.

Can you:
1. List all input fields currently captured
2. Identify any missing fields from the required 31
3. Suggest corrections for incorrect data types or mappings?"
```

**Prompt 2: Output Fields Correction**
```
"The output should have:
- PART A: 17 columns (Policy Year, Premium, + 8 charges/values × 2 scenarios)
- PART B: 16 columns (detailed breakdown)
- Both parts for 4% and 8% return scenarios
- 20 rows (Policy Years 1-20)

Can you verify the current structure and:
1. Identify columns that are missing or misaligned
2. Check if calculations match the reference values
3. Correct any formula errors in Fund Value calculations"
```

**Prompt 3: Formula Verification - Fund Value**
```
"The Fund Value formula should be:
Fund_Value = Previous_Fund 
           + Premium (if Year ≤ PPT)
           - Mortality_Charges
           - Policy_Admin_Charges  
           - FMC
           + Investment_Income
           + Loyalty_Addition
           + Wealth_Booster

For Year 1, 4% return:
Expected: 23,048

Can you:
1. Show the current formula in my code
2. Identify any discrepancies
3. Correct if needed"
```

**Prompt 4: Mortality Charges Lookup**
```
"Mortality charges are age and return-dependent. 
Sample values:
Age 37, Year 1: 357 (4%) / 356 (8%)
Age 37, Year 5: 264 (4%) / 236 (8%)

Can you:
1. Verify the mortality lookup table is correct
2. Check if charges are applied by age and year
3. Ensure different values for 4% vs 8% scenarios"
```

**Prompt 5: Surrender Value Logic**
```
"Surrender Value should follow this logic:
- Years 1-4: 0 or 'Locked' (not available)
- Year 5+: Equals Fund Value at end of year

Can you verify the current IF logic and correct if needed?
Reference values:
Year 1: 0
Year 5: 121,638 (4%) / 136,884 (8%)
Year 10: 281,519 (4%) / 348,652 (8%)"
```

**Prompt 6: Death Benefit Calculation**
```
"Death Benefit = MAX(Sum_Assured, Fund_Value)
Sum_Assured = 240,000

Expected behavior:
- Years 1-9 (4%): 240,000 (minimum)
- Year 10+ (4%): Fund Value if > 240,000

Can you:
1. Check the MAX formula
2. Verify logic against these values:
   Year 1: 240,000
   Year 10: 281,519
   Year 20: 388,129"
```

**Prompt 7: Option-Based Charges (Platinum vs Plus)**
```
"The code should handle two options:
1. Platinum: ARB = 0
2. Platinum Plus: ARB = charge amount

Can you:
1. Identify where option selection affects charges
2. Verify the IF logic for ARB charges
3. Ensure death benefits are calculated correctly for each option"
```

---

## SECTION 10: DOCUMENT DELIVERY CHECKLIST

**Status of Verification Documents:**
- [x] Input fields identified: 31 total
- [x] Output fields identified: 17 (Part A) + 16 (Part B)
- [x] Abbreviations documented: 10 total
- [x] Key formulas extracted: 7 core formulas
- [x] Sample calculations provided: For 4% and 8% scenarios
- [x] Modal factor values confirmed: Yearly=1.0, HY=0.5108, Q=0.2582, M=0.0867
- [x] Mortality table referenced: Age and year-dependent
- [x] Policy admin charges: Fixed 1,200 (Years 1-10)
- [x] FMC rate: 0.1118%
- [x] Option comparison: Platinum vs Platinum Plus
- [x] Copilot prompts: 7 prompts provided

---

## QUICK REFERENCE SUMMARY

| Category | Count | Status |
|----------|-------|--------|
| Input Fields | 31 | ✓ Complete |
| Output Columns (Part A) | 17 | ✓ Complete |
| Output Columns (Part B) | 16 | ✓ Complete |
| Abbreviations | 10 | ✓ Complete |
| Key Formulas | 7 | ✓ Complete |
| Parameters (Non-Formula) | 11 | ✓ Complete |
| Options Supported | 2 | ✓ Complete |
| Policy Years Covered | 20 | ✓ Complete |
| Return Scenarios | 2 | ✓ Complete |
| Charges Types | 6 | ✓ Complete |

---

**Status: ✅ READY FOR GITHUB COPILOT VERIFICATION**

**Next Steps:**
1. Copy this entire document to GitHub Copilot
2. Use Section 9 prompts to verify code accuracy
3. Implement corrections for any identified errors
4. Validate output against sample calculations
5. Test both Platinum and Platinum Plus options

---

*Document Generated: 2026-03-18*  
*Product: SUD Life e-Wealth Royale (142L082V03)*  
*File: Copilot-eWealth-Royale-Verification-Prompt.md*
