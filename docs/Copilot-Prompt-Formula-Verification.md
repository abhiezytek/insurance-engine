# GITHUB COPILOT PROMPT - Century Income BI Formula Verification & Code Generation

**Use this prompt with GitHub Copilot to verify and correct formulas**

---

## COPILOT INSTRUCTION PROMPT

```
You are an expert insurance domain analyst and Excel formula specialist. 
I'm building the Benefit Illustration (BI) report generation system for 
"SUD Life Century Income" insurance product.

CRITICAL PARAMETERS (Always use these abbreviations in code):
- iPRM: Annualized Premium (Input!B9)
- iPRM_PP: Premium Paying Term (Input!B7) - ranges 7 to 12 years
- iBP: Base Plan / Policy Term (Input!B8) - ranges 15 to 25 years
- iAGE: Age of Life Assured (Input!B3) - ranges 18 to 65
- SA: Sum Assured (Input!B16) - calculated as Annual Premium × 10
- IPRM: Installment Premium (Input!B23) - premium per installment
- GI: Guaranteed Income Rate (Input!B17) - 10%, 30%, or 105%
- MB: Mortality Benefit - from separate MB sheet by age/option
- SF: Surrender Factor - from separate SF sheet by policy year/option
- GSV: Guaranteed Surrender Value (Output!G)
- SSV: Special Surrender Value (Output!H)
- PY: Policy Year (1-25)
- D2: Option Code (1=Immediate Income, 2=Deferred Income, 3=Twin Income)

MODAL FACTOR LOOKUP (MUST be exact):
Yearly: 1.0
Half Yearly: 0.5108
Quarterly: 0.2582
Monthly: 0.0867

OPTION CODE MAPPINGS (MUST be exact):
1 = Immediate Income      → GI%=10%, Loyalty%=30%, Income_Start=1, Interval=1yr
2 = Deferred Income       → GI%=30%, Loyalty%=10%, Income_Start=PPT+1, Interval=1yr
3 = Twin Income           → GI%=105%, Loyalty%=0%, Income_Start=10, Interval=5yr

KEY FORMULAS FOR VERIFICATION:

1. Annual Premium (Input!B11):
   = B9 × I8 × J8
   Where: B9=iPRM, I8=Modal Factor, J8=No.of Payments
   Expected Result: 50000 × 0.0867 × 12 = 52,020

2. Installment Premium with Loading (Input!B23):
   = ROUND(iPRM × I8 + I8 × IF(B12="No", 1.5 × SA / 1000, 0), 0)
   Logic: Base premium + loading if Standard Age Proof=No
   Loading: 1.5 paise per Rs.1000 SA

3. Survival Benefit (Output!C):
   = IF(A > iBP, "", 
       IF(A < Income_Start, 0, 
          IF(OR(D2=1, D2=2), 
             iPRM × GI, 
             iPRM × Option3_Factor) 
          + IF(D2=2, 
             IF(AND(A >= In_St_Yr, A <= Income_End), 
                GI × iPRM × In_GI × (A-(In_St_Yr-1)), 0), 0)))
   
   Logic:
   - Blank after policy term ends (A > iBP)
   - 0 before income starts (A < Income_Start)
   - Option 1,2: iPRM × GI%
   - Option 3: iPRM × lookup factor
   - Option 2 only: add increasing benefit

4. Death Benefit (Output!F):
   = IF(A > iBP, "", MAX(SA, I, 1.05 × (B11 × IF(A > iPRM_PP, iPRM_PP, A))))
   
   Logic: Maximum of:
   - Sum Assured (SA)
   - Surrender Value (column I)
   - 105% of premiums paid

5. Guaranteed Surrender Value (Output!G):
   = IF(A > iBP, "", MAX(0, (B11 × IF(A > iPRM_PP, iPRM_PP, A) × GSV_Factor) - Sum(Survival_Benefits) - Sum(Loyalty_Benefits)))
   
   Logic: 
   - Premiums Paid × GSV Factor from SF sheet
   - Minus benefits already paid (Survival + Loyalty)
   - Minimum 0

6. Special Surrender Value (Output!H):
   = IF(A > iBP, "", 
       (Sum(B_Premiums)/Sum(Total_Premiums) × MB × MB_SSV_Factor) +
       (Sum(B_Premiums)/Sum(Total_Premiums) × Current_Income × Income_SSV_Factor))
   
   Logic:
   - Premium Coverage Ratio × MB × MB SSV Factor
   - Plus: Premium Coverage Ratio × Current Year Income × Income SSV Factor
   - Option-specific SSV tables (SF sheet columns)

7. Maximum Surrender Value (Output!I):
   = IF(A > iBP, "", MAX(G, H))

8. Maturity Benefit (Output!E):
   = IF(A > iBP, "", IF(A = iBP, MB, 0))
   
   Logic: Payable only in final year (Year = Policy Term)

CONDITIONAL LOGIC REQUIREMENTS:
✓ All Output calculations MUST be blank (empty string "") when Year > Policy Term
✓ Premium column shows value only during Premium Paying Term
✓ Survival Benefit follows option-specific income patterns
✓ Loyalty Benefit applies ONLY to Options 1 & 2 (0 for Option 3)
✓ Maturity Benefit payable ONLY in year = Policy Term
✓ Death Benefit = MAX of three components (never less than 105% premiums)

SHEET REFERENCES:
- Input Sheet: B2:B23 (all input parameters and calculations)
- Output Sheet: A29:I53 (25 policy years × 9 columns)
- SF Sheet: Surrender factor tables by PPT-PT combination
- MB Sheet: Mortality benefit tables by age and option

VERIFY AGAINST THESE SAMPLE VALUES:
For Twin Income Option, Age 35, Premium 50,000, PPT 12, PT 25:
- Year 1: Premium=50,000, Survival=0, Death=520,200, GSV=24,615, SSV=24,615
- Year 10: Premium=50,000, Survival=52,500, Death=546,210, GSV=244,326, SSV=426,684
- Year 12: Premium=50,000, Survival=0, Death=655,452, GSV=280,531, SSV=478,934
- Year 25: Premium=Blank, Maturity=912,100, Death=849,165, SSV=849,165

REQUEST:
Please verify:
1. All formulas match exact calculations above
2. Parameter abbreviations (iPRM, iPRM_PP, etc.) used consistently
3. Modal factor and option code mappings are correct
4. Conditional logic handles all edge cases
5. Output calculations reference correct sheets
6. Sample values match expected results

Provide corrected formulas if any discrepancies found, with explanation of change.
```

---

## QUICK COPILOT COMMANDS

### To verify specific formulas:
```
Verify the Annual Premium calculation formula for Century Income BI:
Current: =B9*I8*J8
Parameters: B9=iPRM(50000), I8=Modal Factor(0.0867), J8=No.Payments(12)
Expected Result: 52,020
Is this correct? If not, provide the corrected formula.
```

### To correct a formula:
```
The Survival Benefit formula for Output!C is showing incorrect values.
Current formula attempts to: [paste current formula]
It should: Calculate annual guaranteed income based on option selected
- Option 1,2: iPRM × GI%
- Option 3: iPRM × lookup factor
- Add increasing benefit for Option 2 after income start

Please provide the correct formula with all conditions.
```

### To generate new calculations:
```
Generate a formula for [Purpose] that:
1. Uses these parameters: [list parameters]
2. Applies this logic: [describe logic]
3. References these cells: [list references]
4. Returns format: [currency/number/percentage]
5. Must handle edge cases: [describe conditions]

Include explanation of the formula components.
```

---

## ABBREVIATION CHEAT SHEET FOR COPILOT CONTEXT

| Symbol | Meaning | Usage |
|--------|---------|-------|
| iPRM | Annualized Premium | =iPRM → 50,000 |
| iPRM_PP | Premium Paying Term | =IF(Year>iPRM_PP,"",amount) |
| iBP | Policy Term | =IF(Year>iBP,"",amount) |
| iAGE | Age of Life Assured | OFFSET lookup offset |
| SA | Sum Assured | Death Benefit = MAX(SA, ...) |
| IPRM | Installment Premium | Premium per mode |
| GI | Guaranteed Income % | Survival = iPRM × GI |
| MB | Mortality Benefit table | Sheet reference |
| SF | Surrender Factor table | Sheet reference |
| GSV | Guaranteed Surrender Value | =(Premiums × Factor) - Benefits |
| SSV | Special Surrender Value | Premium ratio × Components |
| PY | Policy Year | 1 to 25 |
| D2 | Option Code | 1, 2, or 3 |

---

## EXCEL SHEET STRUCTURE FOR COPILOT REFERENCE

### Input Sheet Layout:
```
Row 1-10: Modal Factor & Payment Table (H10:J13)
         Mode of Payment | Modal Factor | No. of Payments
         Yearly          | 1.0         | 1
         Half Yearly     | 0.5108      | 2
         Quarterly       | 0.2582      | 4
         Monthly         | 0.0867      | 12

Row 15-22: Premium Benefit Table (H16:K22)
          Channel | Benefit1 | Benefit2 | Benefit3

Row 26-29: Premium Discount/Loading Table (H26:K29)
          Premium Level | Option1% | Option2% | Option3%
          50,000        | 0%       | 0%       | 0%
          100,000       | 3%       | 2.25%    | 3.25%
          200,000       | 3.5%     | 3%       | 4.5%

Row 2-4: Option Mapping (P2:Q4)
        Immediate Income | 1
        Deferred Income  | 2
        Twin Income      | 3

Columns B,D,F,I-L: Calculated Parameters
Rows 29-53: Output Table (25 policy years)
```

### SF Sheet Structure:
```
Columns I-L: Option 1 (Immediate) factors for PPT 7/15, 7/20, 10/20, 10/25
Columns P-S: Option 2 (Deferred) factors
Columns W-Z: Option 3 (Twin) factors
Rows 5+: GSV/SSV factor values by policy year
```

### MB Sheet Structure:
```
Table 1 (A2:E30+): Immediate Income - Ages 18-50+
Table 2 (A49:E80+): Deferred Income - Ages 18-50+
Table 3 (A96:E120+): Twin Income - Ages 18-60+
Each with columns for PPT-PT combinations
```

---

## FINAL COPILOT CHECKLIST

Before generating PDF or code, ask Copilot to verify:

- [ ] **Named Parameters**: All iPRM, iPRM_PP, iBP, iAGE, SA, IPRM, GI references correct
- [ ] **Modal Factor**: Monthly=0.0867, Quarterly=0.2582, Half Yearly=0.5108, Yearly=1.0
- [ ] **Option Codes**: 1=Immediate (GI 10%), 2=Deferred (GI 30%), 3=Twin (GI 105%)
- [ ] **Income Logic**: Option 1 & 2 annual, Option 3 every 5 years; Loyalty only for 1 & 2
- [ ] **Survival Benefit**: Blank after policy term, option-specific calculations
- [ ] **Death Benefit**: MAX(SA, Surrender, 105% Premiums), never decreases
- [ ] **Surrender Values**: GSV deducts benefits, SSV uses premium ratio
- [ ] **Lookup Ranges**: MB sheet tables referenced with OFFSET, SF sheet with conditional columns
- [ ] **Sample Validation**: Year 10 Twin Income matches (Survival=52,500, Death=546,210)
- [ ] **Conditional Blanks**: All columns blank when Year > Policy Term

---

**Document Version:** 1.0  
**Created:** 2026-03-18  
**Purpose:** GitHub Copilot Formula Verification & Code Generation  
**Product:** SUD Life Century Income (142N100V03)

---

## ADDITIONAL CONTEXT FOR COPILOT

**Product Details:**
- Individual Non-linked Non-participating Savings Life Insurance Plan
- 3 Income Options: Immediate (1), Deferred (2), Twin (3)
- PPT Options: 7, 10, 12 years
- PT Options: 15, 20, 25 years
- Premium Range: 50,000 - 1,000,000
- GST Rate: 0% (example shown, may vary)
- Surrender allowed after Year 1, with GSV and SSV calculation

**Common Scenarios:**
1. Immediate Income Option: Income starts immediately from Year 1
2. Deferred Income Option: Income starts in Year (PPT+1), increases 10% annually
3. Twin Income Option: First income Year 10, Second income Year 15/20/25

**Expected Output:**
Benefit Illustration PDF with 25-year table showing year-by-year:
- Premiums payable
- Guaranteed survival income
- Loyalty bonuses
- Death benefits
- Surrender values (GSV and SSV)

All values calculated per Excel formulas and presented in currency format with proper separators.

---

**Save this prompt to your Copilot context or reference it when asking for formula verification.**
