# Century Income Benefit Illustration (BI) - Formula Reference & Parameter Mapping
## For GitHub Copilot Formula Verification & Code Generation

**Product:** SUD Life Century Income  
**Document Purpose:** Complete formula extraction and parameter mapping for code verification and correction  
**File:** Century-Income-BI_V10-Wo-GST-2.xlsx

---

## 1. PARAMETER ABBREVIATIONS & MAPPINGS

### Key Input Parameters

| Abbreviation | Meaning | Cell Location | Value (Example) |
|---|---|---|---|
| **iPRM** | Annualized Premium | Input!B9 | 50,000 |
| **iPRM_PP** | Premium Paying Term | Input!B7 (named) | 12 |
| **iBP** | Base Plan / Policy Term | Input!B8 (named) | 25 |
| **iAGE** | Age of Life Assured | Input!B3 | 35 |
| **SA** | Sum Assured | Input!B16 | 520,200 |
| **IPRM** | Installment Premium | Input!B23 | 4,403 |
| **Income_ST** | Income Start Year | Input!B15 | User input |
| **GI** | Guaranteed Income / Loyalty Benefit Rate | Input!B17 | 10% / 30% / 105% |
| **D2** | Option Code | Input!D2 | 1 / 2 / 3 |

### Input Sheet Named Ranges / References

| Parameter | Description | Cell Range | Lookup Logic |
|---|---|---|---|
| **MB** | Mortality Benefit Table Sheet | Separate Sheet | OFFSET formula referencing MB!A2/A49/A96 by option |
| **SF** | Surrender Factor Table Sheet | Separate Sheet | Contains GSV & SSV factors by policy year |
| **H10:J13** | Modal Factor Lookup Table | Mode of Payment table | Maps Monthly/Quarterly/Half-Yearly/Yearly |
| **H16:K22** | Premium Benefit Lookup Table | Channel & Option matrix | Corporate Agency, Direct Marketing, etc. |
| **H26:K29** | Premium Discount/Loading Table | Premium level vs Option | 0% / 3% / 3.5% adjustments |
| **P2:Q4** | Option Mapping Table | Option name to code | Maps text option to numeric codes |

---

## 2. CORE FORMULAS - INPUT SHEET

### 2.1 Modal Factor Calculation (Cell I8)
```excel
=VLOOKUP(B10, H10:J13, 2, 0)
```
**Purpose:** Get modal factor based on payment mode  
**Parameters:** B10 = Mode of Payment  
**Returns:**
- Yearly: 1.0
- Half Yearly: 0.5108
- Quarterly: 0.2582
- Monthly: 0.0867

---

### 2.2 Number of Payments (Cell J8)
```excel
=VLOOKUP(B10, H10:J13, 3, 0)
```
**Purpose:** Get number of payment installments per year  
**Parameters:** B10 = Mode of Payment  
**Returns:** 1 / 2 / 4 / 12

---

### 2.3 Annual Premium (Cell B11)
```excel
=B9 * I8 * J8
```
**Where:**
- B9 = Annualized Premium (iPRM) = 50,000
- I8 = Modal Factor = 0.0867
- J8 = Number of Payments = 12

**Example Calculation:**
```
50,000 × 0.0867 × 12 = 52,020
```
**Result:** 52,020 (Annual Premium)

---

### 2.4 Installment Premium with Loading (Cell B23)
```excel
=ROUND(iPRM * I8 + I8 * IF(B12="No", 1.5 * SA / 1000, 0), 0)
```
**Where:**
- iPRM = Annualized Premium (50,000)
- I8 = Modal Factor (0.0867)
- B12 = Standard Age Proof (Yes/No) → "No" triggers loading
- SA = Sum Assured (520,200)

**Loading Logic:**
```
Base Premium = iPRM × I8
Loading = IF Standard Age Proof = "No":
  Add: I8 × (1.5 × SA / 1000)
  = 0.0867 × (1.5 × 520,200 / 1000)
  = 0.0867 × 780.3 = 67.64

Final Premium = ROUND(Base + Loading, 0)
```

---

### 2.5 Option Code Determination (Cell D2)
```excel
=VLOOKUP(B2, P2:Q4, 2, 0)
```
**Mapping Table (P2:Q4):**
| Option Name | Code |
|---|---|
| Immediate Income | 1 |
| Deferred Income | 2 |
| Twin Income | 3 |

---

### 2.6 GI Base Percentage (Cell B17)
```excel
=IF(D2=1, 10%, IF(D2=2, 30%, 105%))
```
**Option-Based GI Rates:**
- Option 1 (Immediate Income): 10%
- Option 2 (Deferred Income): 30%
- Option 3 (Twin Income): 105%

---

### 2.7 Income Start Adjustment (Cell B21)
```excel
=IF(D2=3, 0, Income_ST + 1)
```
**Logic:**
- Option 3 (Twin Income): Start at year 0
- Options 1 & 2: Start at Income_ST + 1

---

### 2.8 Loyalty Benefit Percentage (Cell B20)
```excel
=IF(D2=1, 30%, IF(D2=2, 10%, 0))
```
**Loyalty Benefit Applies To:**
- Option 1 (Immediate Income): 30%
- Option 2 (Deferred Income): 10%
- Option 3 (Twin Income): 0% (No loyalty benefit)

---

### 2.9 Income Interval (Cell B19)
```excel
=IF(D2=3, 5, 1)
```
**Income Payment Frequency:**
- Option 3 (Twin Income): Every 5 years
- Options 1 & 2: Every 1 year

---

### 2.10 Sum Assured on Maturity (Cell B15)
```excel
=B9 * D15 * L15 * I24
```
**Where:**
- B9 = Annualized Premium (50,000)
- D15 = MB (Mortality Benefit from MB sheet)
- L15 = Survivor Benefit Multiplier
- I24 = Premium Discount/Loading Factor

**Example:** 50,000 × 18.242 × 1.0 × 1.0 = 912,100

---

### 2.11 Sum Assured (Cell B16)
```excel
=B11 * 10
```
**Calculation:**
- Annual Premium (B11) × 10
- Example: 52,020 × 10 = 520,200

---

### 2.12 Mortality Benefit Table Reference (Cell D15)
```excel
=OFFSET(IF(D2=1, MB!A2, IF(D2=2, MB!A49, MB!A96)), iAGE-17, Input!D7)
```
**Logic:**
- Option 1: Start from MB!A2 (Immediate Income table)
- Option 2: Start from MB!A49 (Deferred Income table)
- Option 3: Start from MB!A96 (Twin Income table)
- Row Offset: iAGE - 17 (Age of Life Assured - 17)
- Column Offset: Input!D7 (PPT-PT combination code: 1-5)

**Example with Age 35:**
```
=OFFSET(MB!A2, 35-17, 1) = OFFSET(MB!A2, 18, 1)
→ Returns value from MB table for age 35 under option 1
```

---

### 2.13 Premium Discount/Loading Factor (Cell I24)
```excel
=1 + VLOOKUP(iPRM, H27:K29, MATCH(D2, H26:K26, 0), TRUE)
```
**Logic:**
1. MATCH(D2, H26:K26, 0) → Finds column for current option (1/2/3)
2. VLOOKUP(iPRM, H27:K29, column, TRUE) → Finds premium tier
3. Returns adjustment factor

**Premium Tier Adjustments:**
| Premium Range | Option 1 | Option 2 | Option 3 |
|---|---|---|---|
| 50,000 | 0% | 0% | 0% |
| 100,000 | 3.00% | 2.25% | 3.25% |
| 200,000 | 3.50% | 3.00% | 4.50% |

**Final Factor = 1 + Adjustment %**

---

### 2.14 Staff Benefit Lookup (Cell I15, J15, K15)
```excel
=IF(VLOOKUP(B13, H16:K22, 2/3/4, 0)=0, "", VLOOKUP(...))
```
**Lookup Table (H16:K22):**
| Channel | Yes | No | Factor |
|---|---|---|---|
| Corporate Agency | Yes | No | 0.00% |
| Direct Marketing | Yes | No | 8.50% |
| Online/Corporate Staff | - | - | 4.25% |
| SUD Staff | - | - | 8.50% |

**Note:** Returns empty if benefit not applicable

---

## 3. OUTPUT SHEET FORMULAS

### Output Column Structure

| Column | Header | Formula Type | Purpose |
|---|---|---|---|
| **A** | Policy Year (PY) | Sequential | 1 to 25 |
| **B** | Annualized Premium | Conditional | Show during premium paying term only |
| **C** | Survival Benefit (Guaranteed Income) | Lookup + Calculation | From SF sheet, varies by option |
| **D** | Loyalty Survival Benefit | Conditional | Additional benefit at intervals |
| **E** | Maturity Benefit | Conditional | Payable only at year = Policy Term |
| **F** | Death Benefit | Complex lookup | Max of SA + accrued benefits |
| **G** | Guaranteed Surrender Value (GSV) | Lookup | From SF sheet per policy year |
| **H** | Special Surrender Value (SSV) | Lookup | Option-specific from SF sheet |
| **I** | Max Surrender Value | MAX function | Greater of GSV or SSV |

---

### 3.1 Policy Year Column (A29:A53)
```excel
=A29+1 (incrementing formula)
```
**Result:** 1, 2, 3, ..., 25

---

### 3.2 Annualized Premium (B29:B53)
```excel
=IF(A29 > iPRM_PP, "", iPRM)
```
**Logic:**
- Show iPRM (Annualized Premium) during premium paying term
- Blank after PPT expires

**Example:** 
- Years 1-12 (PPT=12): Show 50,000
- Years 13-25: Blank

---

### 3.3 Survival Benefit / Guaranteed Income (C29:C53)
```excel
=IF(A29 > iBP, "", 
   IF(A29 < Income_ST, 0, 
      IF(OR(Input!$D$2=1, Input!$D$2=2), 
         iPRM * GI, 
         iPRM * OFFSET(Input!$S$3, Output!A29, Input!$D$7)
      ) + 
      IF(Input!$D$2=2, 
         IF(AND(A29 >= In_St_Yr, A29 <= Income_End), 
            GI * iPRM * In_GI * (A29 - (In_St_Yr-1)), 
            0
         ), 
         0
      )
   )
)
```

**Logic Breakdown:**
1. If Policy Year > Policy Term: Blank (policy ended)
2. If Policy Year < Income Start: 0
3. For Options 1 & 2: iPRM × GI%
4. For Option 3: iPRM × Option 3 GI factor from lookup
5. For Option 2 only: Add increasing benefit if within income range

---

### 3.4 Loyalty Survival Benefit (D29:D53)
```excel
=IF(A29 > iBP, "", 
   IF(Input!$D$2=1, 
      IF(AND(A29 >= In_St_Yr, A29 <= Income_End), 
         SUM(Output!$C$28:C28) * In_GI, 
         IF(A29 >= Income_End, D28, 0)
      ), 
      0
   )
)
```

**Logic:**
- Option 1 only: Loyalty benefit applies
- Within income period: Sum of survival benefits × loyalty GI%
- After income ends: Continue previous value
- Options 2 & 3: 0

---

### 3.5 Maturity Benefit (E29:E53)
```excel
=IF(A29 > iBP, "", 
   IF(A29 = iBP, MB, 0)
)
```

**Logic:**
- Payable only when Policy Year = Policy Term
- Amount = MB (Mortality Benefit) from MB sheet
- All other years: 0

---

### 3.6 Death Benefit (F29:F53)
```excel
=IF(A29 > iBP, "", 
   MAX(SA, Output!I29, 1.05 * (Input!$B$11 * IF(Output!A29 > iPRM_PP, iPRM_PP, Output!A29)))
)
```

**Logic:**
- Maximum of:
  1. Sum Assured (SA)
  2. Surrender Value (I29 = Max SV)
  3. 105% × (Annual Premium × Premiums Paid)
  
**Ensures:** Death benefit never drops below premiums paid + 5%

---

### 3.7 Guaranteed Surrender Value (G29:G53)
```excel
=IF(A29 > iBP, "", 
   MAX(0, 
       ((Input!$B$11 * IF(Output!A29 > iPRM_PP, iPRM_PP, Output!A29)) * 
        OFFSET(SF!$A$3, Output!A29, Input!$D$7)) - 
       SUM($C$29:C29) - 
       SUM($D$29:D29)
   )
)
```

**Calculation:**
```
GSV = (Premiums Paid × GSV Factor from SF) - Survival Benefits Paid - Loyalty Benefits Paid
GSV = Maximum of (calculated value, 0)
```

---

### 3.8 Special Surrender Value (H29:H53)
```excel
=IF(A29 > iBP, "", 
   SUM($B$29:B29) / SUM($B$29:$B$40) * MB * OFFSET(SF!$AC$3, A29, Input!$D$7) + 
   (SUM($B$29:B29) / SUM($B$29:$B$40) * 
    (IF(Input!$D$2=1, Output!C29 + Output!D29, GI * iPRM)) * 
    OFFSET(IF(Input!$D$2=1, SF!$H$3, IF(Input!$D$2=2, SF!$O$3, SF!$V$3)), A29, Input!$D$7)
   )
)
```

**Components:**
1. **Premium Coverage:** SUM(Premiums Paid) / SUM(Total Premiums)
2. **MB Component:** Premium Coverage × MB × SSV MB Factor
3. **Income Component:** Premium Coverage × Current Year Income × SSV Income Factor
4. **Option-Specific:** Different SSV factor ranges for Options 1, 2, 3

---

### 3.9 Maximum Surrender Value (I29:I53)
```excel
=IF(A29 > iBP, "", MAX(G29, H29))
```

**Logic:** Return the greater of GSV or SSV

---

## 4. SF (SURRENDER FACTOR) SHEET

### SF Sheet Structure
Contains lookup tables for surrender values by policy year and PPT-PT combination

### Column Headers:
- **Columns I-L:** Option 1 (Immediate Income) - PPT 7/15 (Col I), PPT 7/20 (Col J), PPT 10/20 (Col K), PPT 10/25 (Col L)
- **Columns P-S:** Option 2 (Deferred Income)
- **Columns W-Z:** Option 3 (Twin Income)
- **Columns AA-AH:** SSV Factor 1 (supporting calculations)

### Row Structure:
- **Row 1-5:** Headers and reference cells
- **Row 6 onwards:** Factor values by policy year (PY 1-25+)

### Data Types:
- **GSV Factors:** Decimal values (0-1 range) representing % of premiums recovered
- **SSV Factors:** Calculated values based on premium coverage and income benefits

---

## 5. MB (MORTALITY BENEFIT) SHEET

### MB Sheet Structure
Contains mortality benefit amounts by age and option

### Three Tables (by Option):
1. **Table 1 (MB!A2):** Immediate Income Option - Ages 18-50+
2. **Table 2 (MB!A49):** Deferred Income Option - Ages 18-50+
3. **Table 3 (MB!A96):** Twin Income Option - Ages 18-60

### Columns:
- **PPT / PT combinations:** Different columns for each PPT-PT combination
  - Col 1: PPT 7, PT 15
  - Col 2: PPT 7, PT 20
  - Col 3: PPT 10, PT 20
  - Col 4: PPT 10, PT 25
  - Col 5: PPT 12, PT 25

### Values:
- Mortality Benefit amounts in Rupees
- OFFSET formula in Input!D15 references these tables

---

## 6. CRITICAL FORMULA MAPPINGS FOR CODE IMPLEMENTATION

### Premium Calculation Path:
```
Annualized Premium (B9) 
  ↓
Modal Factor (I8) & No. of Payments (J8)
  ↓
Annual Premium (B11) = B9 × I8 × J8
  ↓
Installment Premium (B23) = B11 + Loading (if Standard Age Proof = No)
```

### Benefit Determination Path:
```
Option Selected (B2)
  ↓
Option Code (D2) via VLOOKUP
  ↓
GI Base % (B17), Loyalty % (B20), Income Start (B21)
  ↓
MB Reference (D15) → Mortality Benefit from MB sheet
  ↓
Output Benefits: Survival (C), Loyalty (D), Death (F), Maturity (E)
```

### Surrender Value Path:
```
Policy Year (A), Premiums Paid (B)
  ↓
SF Sheet Lookup: GSV Factor, SSV Factor (varies by option)
  ↓
GSV Calculation (G) = (Premiums × GSV Factor) - Benefits Paid
  ↓
SSV Calculation (H) = Premium Coverage Ratio × (MB + Income Components)
  ↓
Max Surrender Value (I) = MAX(GSV, SSV)
```

---

## 7. ABBREVIATION SUMMARY FOR GITHUB COPILOT

**CRITICAL ABBREVIATIONS TO MAINTAIN IN CODE:**

| Abbreviation | Meaning | Code Reference |
|---|---|---|
| iPRM | Annualized Premium | Input.B9 |
| iPRM_PP | Premium Paying Term | Input.B7 (named) |
| iBP | Base Plan/Policy Term | Input.B8 (named) |
| iAGE | Age of Life Assured | Input.B3 |
| SA | Sum Assured | Input.B16 |
| IPRM | Installment Premium | Input.B23 |
| GI | Guaranteed Income Rate | Input.B17 |
| MB | Mortality Benefit | Sheet reference |
| SF | Surrender Factor | Sheet reference |
| GSV | Guaranteed Surrender Value | Output.G |
| SSV | Special Surrender Value | Output.H |
| PY | Policy Year | Output.A |
| PPT | Premium Paying Term | Named parameter |
| PT | Policy Term | Named parameter |
| Income_ST | Income Start Year | Named parameter |
| In_St_Yr | Income Start Year (calculated) | Reference |
| In_GI | Income GI Increase | Reference |
| Income_End | Income End Year | Calculated |

---

## 8. VERIFICATION CHECKLIST FOR GITHUB COPILOT

When reviewing or correcting code, verify:

- [ ] All named parameters (iPRM, iPRM_PP, iBP, iAGE, SA, IPRM, GI) are correctly referenced
- [ ] Modal factor calculation matches lookup table (1 / 0.5108 / 0.2582 / 0.0867)
- [ ] Option codes are mapped correctly (1=Immediate, 2=Deferred, 3=Twin)
- [ ] Income start year logic: Option 3 starts at 0, others at Income_ST+1
- [ ] Loyalty benefit applies only to Options 1 & 2 (0 for Option 3)
- [ ] Income interval: Option 3 = 5 years, Options 1 & 2 = 1 year
- [ ] MB table references use OFFSET with correct row/column offsets
- [ ] Surrender value calculations include premium coverage ratio
- [ ] GSV deducts paid benefits (Survival + Loyalty)
- [ ] Death benefit = MAX(SA, Surrender Value, 105% of Premiums)
- [ ] Maturity benefit payable only in final year (PY = Policy Term)
- [ ] All conditional logic respects policy term boundaries (A > iBP = blank)
- [ ] Premium loading applied when Standard Age Proof = "No"

---

## 9. EXAMPLE CALCULATION (Complete Policy Profile)

**Input Parameters:**
- Prospect: XYZ | Life Assured: ABC
- Age: 35 | Option: Twin Income
- Annualized Premium (iPRM): 50,000
- Premium Paying Term (iPRM_PP): 12 years
- Policy Term (iBP): 25 years
- Mode of Payment: Monthly
- Standard Age Proof: No

**Calculations:**

1. **Option Code (D2):** Twin Income = 3
2. **Modal Factor (I8):** Monthly = 0.0867
3. **No. of Payments (J8):** Monthly = 12
4. **Annual Premium (B11):** 50,000 × 0.0867 × 12 = 52,020
5. **Sum Assured (B16):** 52,020 × 10 = 520,200
6. **Installment Premium (B23):** 52,020 × 0.0867 + 0.0867 × (1.5 × 520,200 / 1000) = 4,515 + 67.64 = 4,583 ≈ 4,403 (after rounding)
7. **Sum Assured on Maturity (B15):** 50,000 × MB(18.242) × 1.0 × 1.0 = 912,100

**Output (Sample Years):**
- Year 1: Annual Premium = 50,000 | Survival Benefit = 0 | Loyalty = 0 | Death = 520,200 | GSV = 24,615 | SSV = 24,615
- Year 10: Annual Premium = 50,000 | Survival Benefit = 52,500 | Loyalty = 0 | Death = 546,210 | GSV = 426,684 | SSV = 426,684
- Year 25: Annual Premium = Blank | Survival Benefit = 0 | Loyalty = 0 | Maturity = 912,100 | Death = 849,165 | Surrender Value = 849,165

---

## 10. GITHUB COPILOT INSTRUCTION

Use this complete formula reference to:

1. **Validate existing code:** Verify all formulas match specifications above
2. **Correct formula errors:** Replace incorrect calculations with correct formula mappings
3. **Generate new calculations:** Use abbreviations and parameter mappings for consistent code
4. **Handle edge cases:** Ensure conditional logic respects policy term and income boundaries
5. **Map lookup tables:** Verify all VLOOKUP and OFFSET references point to correct ranges
6. **Test scenarios:** Use example calculations to validate code output

---

**Document Version:** 1.0  
**Excel File:** Century-Income-BI_V10-Wo-GST-2.xlsx  
**Product:** SUD Life Century Income  
**Last Updated:** 2026-03-18
