# Century Income BI - OUTPUT PDF REQUIREMENTS & DATA MAPPING
## Complete Specification for Benefit Illustration Report Generation

**Product Header:** SUD Life Century Income  
**Product Code:** 142N100V03 (Unique Identification Number)  
**Tag Line:** Individual Non-linked Non-participating Savings Life Insurance Plan

---

## 1. OUTPUT PDF SECTIONS STRUCTURE

### Section 1: Policy Details Header
```
Name of Prospect/Policyholder:  [XYZ]
Age:                            [35]
Name of Life Assured:           [ABC]
Age:                            [35]
Policy Term:                    [25 years]
Premium Payment Term:           [12 years]
Amount of Instalment Premium:   [4,403]
Mode of Payment of Premium:     [Monthly]

Proposal No.:                   [To be filled]
Name of the Product:            SUD Life Century Income
Tag Line:                       Individual Non-linked Non-participating Savings Life Insurance Plan
Unique Identification Number:   142N100V03
GST Rate (1st Year):            0.00%
GST Rate (2nd Year Onwards):    0.00%
```

### Section 2: Policy Details Subsection
```
Policy Option (Income Option):  [Twin Income / Base Plan]
Sum Assured Rs.                 520,200
Sum Assured on Death (inception) 520,200
```

### Section 3: Premium Summary Table
```
                                    Base Plan    Riders    Total Instalment Premium
Instalment Premium without GST      4,403        NA        4,403
Instalment Premium with FY GST      4,403        NA        4,403
Instalment Premium with 2Y+ GST     4,403        NA        4,403
```

### Section 4: Benefit Illustration Table (Main BI)

**Table Headers:**
```
Policy Year | Annualized   | Guaranteed          | Non Guaranteed | Surrender Value
            | Premium      | ─────────────────── | ────────────── | ──────────────
            |              | Survival | Loyalty  | Maturity Death | GSV      SSV    Max(GSV/SSV)
            |              | Benefit  | Benefit  |                |
```

**Table Columns (Detailed):**

| Column | Header | Formula Source | Data Type | Condition |
|---|---|---|---|---|
| A | Policy Year (PY) | Output!A (1-25) | Integer | 1-25 sequence |
| B | Annualized Premium | Output!B = IF(A>iPRM_PP,"",iPRM) | Currency | Blank after PPT |
| C | Survival Benefit | Output!C (from SF lookup) | Currency | Varies by option & year |
| D | Loyalty Survival Benefit | Output!D (conditional) | Currency | Option 1&2 only, certain years |
| E | Maturity | Output!E (MB value at year=PT) | Currency | Year 25 only (912,100) |
| F | Death Benefit | Output!F (MAX formula) | Currency | All years |
| G | Guaranteed Surrender Value | Output!G (SF lookup - benefits) | Currency | GSV Formula |
| H | Special Surrender Value | Output!H (option-specific) | Currency | SSV Formula |
| I | Max Surrender Value | Output!I = MAX(G,H) | Currency | Greater of GSV/SSV |

### Section 5: Footnotes/Notes Section

```
NOTES:
- Annualized Premium excludes:
  • Underwriting extra premium
  • Frequency loadings on premiums
  • Premiums paid towards riders (if any)
  • Goods and Service Tax

- Surrender Benefit:
  Policy surrender allowed after end of first policy year provided one full year's premiums paid.
  During Policy Term: Surrender Value = Higher of GSV or SSV

- Income Options Policy:
  • Option 1 (Immediate Income): Loyalty benefit continues only when policy inforce and 
    up to premium payment term
  • Option 2 (Deferred Income): Income starts in year 11, increases annually
  • Option 3 (Twin Income): Income starts in year 10, increases every 5 years

- Rider Availability:
  No riders available under the plan

- Policy Values:
  Values shown are end-of-year (assume events occur at year end)

- Tax Benefits:
  Premiums and benefits eligible for tax benefits per prevailing tax laws.
  Consult tax advisor for details.

- GST:
  Goods & Services Tax and applicable cess (if any) charged on premiums per prevailing tax laws.
  Subject to change in tax rate.
```

### Section 6: Terms & Conditions

```
1. Surrender Benefit
   Surrender allowed after end of first policy year provided one full years' premiums paid.
   During Policy Term: Surrender Value = Higher of GSV or SSV.

2. Income Option Policy
   - Option 1: Loyalty benefit continues only when policy inforce & up to premium payment term
   - Option 2: Increasing benefit structure
   - Option 3: Benefits at 5-year intervals

3. Rider Availability
   No riders available under the plan.

4. Policy Values Timing
   Values shown are at year end (assumed events occur at year end).

5. Tax Benefits
   Premiums and benefits eligible for tax benefits per prevailing tax laws.
   Consult tax advisor.

6. Proposal Deposit
   Proposal deposit receipt does not bind company to accept risk.

7. About Star Union Dai-ichi Life Insurance Company Limited
   SUD Life is the company name. 'SUD Life Century Income' is plan name only.
   Does not indicate quality, prospects, or returns.

8. Goods & Services Tax
   GST and applicable cess charged on premiums per prevailing tax laws.
   Subject to change in tax rate.

9. Further Information
   For details on risk factors, terms & conditions, read Sales brochure carefully.
   In case of clarification, consult insurance advisor or call office.
```

### Section 7: Declaration Section

```
AGENT/INTERMEDIARY DECLARATION:
I _____________________ (Name), have explained the premiums and benefits under 
the product fully to the prospect/policyholder.

Place: ________________________
Date:  ________________________
Signature of Agent/Intermediary/Official: ________________________

PROSPECT/POLICYHOLDER ACKNOWLEDGEMENT:
I _________________________ (Name), having received the information with respect 
to the above, have understood the above statement before entering into the contract.

Place: ________________________
Date:  ________________________
Signature of Prospect/Policyholder: ________________________
```

---

## 2. BENEFIT ILLUSTRATION DATA MAPPING (POLICY LEVEL)

### Header Information Mapping
```
Field                           Source Excel Cell    Lookup/Formula
─────────────────────────────────────────────────────────────────
Prospect Name                   Input!B6             Direct value
Policyholder Name               Input!B6             Direct value
Age (Prospect)                  Input!B5             Direct value
Age (Life Assured)              Input!B3             Direct value
Name of Life Assured            Input!B4             Direct value
Policy Term                     Input!B8 (iBP)       Direct value
Premium Payment Term            Input!B7 (iPRM_PP)   Direct value
Installment Premium             Input!B23 (IPRM)     Calculated value
Mode of Payment                 Input!B10            Direct value
Proposal No.                    Manual input         User entry field
Product Name                    Static text          "SUD Life Century Income"
Tag Line                        Static text          "Individual Non-linked Non-participating..."
Unique ID                       Static text          "142N100V03"
GST 1st Year                    Static value         "0.00%"
GST 2nd+ Year                   Static value         "0.00%"
```

### Policy Details Subsection Mapping
```
Field                           Source              Formula/Lookup
─────────────────────────────────────────────────────────────────
Income Option                   Input!B2            Direct value (Twin Income/Immediate/Deferred)
Base Plan Label                 Static text         "Base Plan"
Sum Assured (Base)              Input!B16 (SA)      Direct value
Sum Assured on Death            Input!B16 (SA)      Same as Sum Assured
```

### Premium Summary Table Mapping
```
Field                           Source              Calculation
─────────────────────────────────────────────────────────────────
Base Plan Premium (no GST)      Input!B23 (IPRM)    Direct value
Base Plan Premium (with GST)    Input!B23 (IPRM)    Same (GST = 0%)
Riders                          Static text         "NA" (No riders)
Total Premium (no GST)          Input!B23 (IPRM)    Direct value
Total Premium (1st Yr GST)      Input!B23 (IPRM)    Direct value + 0% GST
Total Premium (2nd+ Yr GST)     Input!B23 (IPRM)    Direct value + 0% GST
```

### Main Benefit Illustration Table Mapping (Rows 1-25)

**Row Mapping for Each Policy Year:**

```
Year 1:
├─ PY (A29):              1
├─ Premium (B29):         IF(A29>iPRM_PP, "", 50000) → "50,000"
├─ Survival (C29):        Option-specific formula → "0"
├─ Loyalty (D29):         Option-specific formula → "0"
├─ Maturity (E29):        IF(A29=iBP, MB, 0) → "0"
├─ Death (F29):           MAX formula → "520,200"
├─ GSV (G29):             SF lookup formula → "24,615"
├─ SSV (H29):             SF lookup formula → "24,615"
└─ Max SV (I29):          MAX(G29,H29) → "24,615"

Year 10:
├─ PY (A38):              10
├─ Premium (B38):         IF(A38>iPRM_PP, "", 50000) → "50,000"
├─ Survival (C38):        [Income starts for Twin] → "52,500"
├─ Loyalty (D38):         [Option 3 = 0] → "0"
├─ Maturity (E38):        IF(A38=25, 912100, 0) → "0"
├─ Death (F38):           MAX formula → "546,210"
├─ GSV (G38):             SF lookup - benefits → "244,326"
├─ SSV (H38):             SF lookup formula → "426,684"
└─ Max SV (I38):          MAX(G38,H38) → "426,684"

Year 12 (Last Premium Year):
├─ PY (A40):              12
├─ Premium (B40):         IF(A40>iPRM_PP, "", 50000) → "50,000"
├─ Survival (C40):        [Income continues] → "0"
├─ Loyalty (D40):         [Option 3 = 0] → "0"
├─ Maturity (E40):        IF(A40=25, 912100, 0) → "0"
├─ Death (F40):           MAX formula → "655,452"
├─ GSV (G40):             SF lookup formula → "280,531"
├─ SSV (H40):             SF lookup formula → "478,934"
└─ Max SV (I40):          MAX(G40,H40) → "478,934"

Year 13 (After Premium Term):
├─ PY (A41):              13
├─ Premium (B41):         IF(A41>iPRM_PP, "", 50000) → "" (Blank)
├─ Survival (C41):        [Income pattern] → "0"
├─ Loyalty (D41):         [Option 3 = 0] → "0"
├─ Maturity (E41):        IF(A41=25, 912100, 0) → "0"
├─ Death (F41):           MAX formula → "655,452"
├─ GSV (G41):             SF lookup formula → "295,263"
├─ SSV (H41):             SF lookup formula → "512,638"
└─ Max SV (I41):          MAX(G41,H41) → "512,638"

Year 25 (Maturity):
├─ PY (A53):              25
├─ Premium (B53):         IF(A53>iPRM_PP, "", 50000) → "" (Blank)
├─ Survival (C53):        [Income ends] → "0"
├─ Loyalty (D53):         [Option 3 = 0] → "0"
├─ Maturity (E53):        IF(A53=iBP, 912100, 0) → "912,100"
├─ Death (F53):           MAX formula → "849,165"
├─ GSV (G53):             SF lookup formula → "246,816"
├─ SSV (H53):             SF lookup formula → "849,165"
└─ Max SV (I53):          MAX(G53,H53) → "849,165"
```

---

## 3. OPTION-SPECIFIC INCOME PATTERNS

### Option 1: Immediate Income
```
Income Starts:      Year 1
Income Interval:    Every year
Loyalty Benefit:    30% of survival benefit (during PPT)
Pattern:
├─ Years 1-10:    0 survival (no income yet per option setting)
├─ Years 10+:     Survival benefit starts
├─ Years 11-PPT:  +30% loyalty bonus
└─ After PPT:     Survival continues, loyalty stops
```

### Option 2: Deferred Income
```
Income Starts:      Year 13 (PPT + 1)
Income Interval:    Every year
Income Growth:      10% annual increase
Loyalty Benefit:    10% of survival benefit (during income period)
Pattern:
├─ Years 1-12:    0 survival (deferral period)
├─ Year 13+:      Survival benefit (base amount)
├─ Years 13+:     Increasing: Amount × (1.1^(Year-12))
└─ Loyalty:       10% × survival when in income + loyalty period
```

### Option 3: Twin Income
```
Income Starts:      Year 10 (immediate) + Year 15 (additional)
Income Interval:    Every 5 years (second income)
First Income:       Year 10
Second Income:      Year 15, 20, 25
Loyalty Benefit:    None (0%)
Pattern:
├─ Years 1-9:     0 survival
├─ Years 10-14:   52,500 annual (first income)
├─ Year 15:       Continue first + second income
├─ Year 20:       Continue first + third income
└─ Year 25:       Continue + maturity benefit
```

---

## 4. DATA FIELD DEFINITIONS FOR PDF OUTPUT

### Input Fields (From Input Sheet)

| Field Name | Cell | Type | Format | Sample Value |
|---|---|---|---|---|
| Prospect Name | B6 | Text | Alphanumeric | XYZ |
| Policyholder Age | B5 | Number | Numeric | 35 |
| Life Assured Name | B4 | Text | Alphanumeric | ABC |
| Life Assured Age | B3 | Number | Numeric | 35 |
| Option Selected | B2 | Text | Lookup | Twin Income |
| PPT | B7 | Number | Numeric | 12 |
| PT | B8 | Number | Numeric | 25 |
| Annual Premium | B9 | Currency | ₹ Format | 50,000 |
| Mode of Payment | B10 | Text | Dropdown | Monthly |
| Standard Age Proof | B12 | Text | Yes/No | No |
| Distribution Channel | B13 | Text | Lookup | Corporate Agency |
| Staff Policy | B14 | Text | Yes/No | No |
| Sum Assured on Maturity | B15 | Currency | ₹ Format | 912,100 |
| Sum Assured (Base) | B16 | Currency | ₹ Format | 520,200 |
| GI Rate | B17 | Percentage | % Format | 105% |
| Income Start | B18 | Number | Numeric | 10 |
| Income Interval | B19 | Number | Numeric | 5 |
| Increase in GI | B20 | Percentage | % Format | 0% |
| Increase in GI Start | B21 | Number | Numeric | 0 |
| Increase in GI Stop | B22 | Number | Numeric | 25 |
| Installment Premium | B23 | Currency | ₹ Format | 4,403 |

### Output Fields (Generated for PDF)

| Field Name | Formula | Type | Format | Conditional |
|---|---|---|---|---|
| Policy Year | Row number | Integer | 1-25 | Always show |
| Annualized Premium | B29:B53 formula | Currency | ₹ Format | Blank after PPT |
| Survival Benefit | C29:C53 formula | Currency | ₹ Format | Varies by option |
| Loyalty Benefit | D29:D53 formula | Currency | ₹ Format | Option 1&2 only |
| Maturity Benefit | E29:E53 formula | Currency | ₹ Format | Year = PT only |
| Death Benefit | F29:F53 formula | Currency | ₹ Format | All years |
| GSV | G29:G53 formula | Currency | ₹ Format | After premium year 1 |
| SSV | H29:H53 formula | Currency | ₹ Format | Varies by year |
| Max Surrender | I29:I53 formula | Currency | ₹ Format | MAX(GSV,SSV) |

---

## 5. FORMATTING SPECIFICATIONS FOR PDF

### Currency Format
- Use ₹ symbol
- Thousand separator: Comma (,)
- Decimal places: 0 (except where specified)
- Example: ₹ 52,020 (not ₹52,020.00)

### Number Format
- Thousand separator: Comma (,)
- Decimal places: Vary by field

### Percentage Format
- Symbol: %
- Decimal places: 2 (except rates table)
- Example: 0.00% or 3.50%

### Table Formatting
- Font: Sans-serif (Arial/Calibri)
- Table borders: Light gray
- Header background: Light blue / Gray
- Alternating row colors: White / Light gray (for readability)
- Column widths: Auto-fit content

### Page Layout
- Page size: A4
- Orientation: Portrait
- Margins: 1 inch all sides
- Headers/Footers: Product name + page number
- Line spacing: 1.5 for body text, single for tables

---

## 6. DATA VALIDATION RULES

Before generating PDF, validate:

| Validation | Rule | Error Message |
|---|---|---|
| Age Range | 18 ≤ Age ≤ 65 | "Age must be between 18 and 65" |
| Premium Range | 50,000 ≤ Premium ≤ 1,000,000 | "Premium must be ≥50,000" |
| PPT Range | PPT ≤ PT | "PPT cannot exceed PT" |
| Option Valid | Option ∈ {1,2,3} | "Invalid option selected" |
| Sum Assured Range | SA > 0 | "Sum Assured must be positive" |
| Income Start | Income_Start ≥ 1 | "Income start must be ≥ 1" |
| Income Interval | Income_Interval ∈ {1,5} | "Interval must be 1 or 5 years" |
| Premium Term | 1 ≤ PPT ≤ 25 | "PPT must be between 1 and 25" |
| Policy Term | 1 ≤ PT ≤ 25 | "PT must be between 1 and 25" |
| GST Rate | 0% ≤ GST ≤ 18% | "Invalid GST rate" |

---

## 7. GITHUB COPILOT GENERATION INSTRUCTIONS

**For PDF Report Generation:**

```
PROMPT for Copilot:
"Generate a benefit illustration PDF report for SUD Life Century Income with:

Input Parameters:
- Prospect Name: [B6]
- Life Assured Age: [B3]
- Option: [B2] → Code: [D2]
- Premium: [B9] → IPRM: [B23]
- PPT: [B7], PT: [B8]
- Sum Assured: [B16]

Output Table (Rows 29-53 from Output sheet):
- Column A: Policy Years 1-25
- Column B: Annualized Premium (show if Year ≤ PPT, else blank)
- Column C: Survival Benefit from C29:C53
- Column D: Loyalty Benefit from D29:D53
- Column E: Maturity (show if Year = PT, else 0)
- Column F: Death Benefit from F29:F53
- Column G: GSV from G29:G53
- Column H: SSV from H29:H53
- Column I: Max(GSV,SSV) from I29:I53

Formatting:
- Currency: ₹ format with comma separators, no decimals
- Table with alternating row colors
- Include footnotes and terms & conditions
- Add signature section

Use abbreviations: iPRM, iPRM_PP, iBP, iAGE, SA, IPRM, GI, MB, SF, GSV, SSV, PY
"
```

---

## 8. COMPLETE CALCULATION EXAMPLE FOR TWIN INCOME OPTION

**Input Data:**
```
Name: XYZ / ABC
Age: 35
Option: Twin Income (Code = 3)
Annual Premium (iPRM): 50,000
PPT (iPRM_PP): 12
PT (iBP): 25
Sum Assured: 520,200
Income Start: 10
Income Interval: 5
```

**Output (Selected Years):**

| PY | Premium | Survival | Loyalty | Maturity | Death | GSV | SSV | Max(GSV,SSV) |
|---|---|---|---|---|---|---|---|---|
| 1 | 50,000 | 0 | 0 | 0 | 520,200 | 24,615 | 24,615 | 24,615 |
| 10 | 50,000 | 52,500 | 0 | 0 | 546,210 | 244,326 | 426,684 | 426,684 |
| 12 | 50,000 | 0 | 0 | 0 | 655,452 | 280,531 | 478,934 | 478,934 |
| 13 | Blank | 0 | 0 | 0 | 655,452 | 295,263 | 512,638 | 512,638 |
| 15 | Blank | 52,500 | 0 | 0 | 655,452 | 272,102 | 587,774 | 587,774 |
| 20 | Blank | 52,500 | 0 | 0 | 694,183 | 240,575 | 694,183 | 694,183 |
| 25 | Blank | 0 | 0 | 912,100 | 849,165 | 246,816 | 849,165 | 849,165 |

---

**Document Version:** 1.0  
**Last Updated:** 2026-03-18  
**Ready for PDF Generation & GitHub Copilot Implementation**
