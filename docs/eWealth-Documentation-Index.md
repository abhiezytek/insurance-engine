# 📋 e-WEALTH ROYALE BI - COMPLETE DOCUMENTATION INDEX
## GitHub Copilot Verification Ready

**Product:** SUD Life e-Wealth Royale (142N100V03)  
**Document Date:** 2026-03-18  
**Status:** Complete & Ready for GitHub Copilot Verification

---

## 🎯 EXECUTIVE SUMMARY

You now have a **complete e-Wealth Royale BI verification package** with:

- ✅ 1 Comprehensive GitHub Copilot Prompt (ready to paste & use)
- ✅ 6 CSV Reference Tables (for quick lookups)
- ✅ Complete Input Field Specification (31 fields)
- ✅ Complete Output Field Specification (32 fields)
- ✅ 12 Critical Formula Definitions
- ✅ 17 Core Parameters with Usage
- ✅ Sample Calculation Values (for testing)
- ✅ Known Issues & Error Prevention Guide
- ✅ Verification Checklist (11-point)

---

## 📦 DELIVERABLES (7 Total Files)

### 1️⃣ GitHub Copilot Prompt (MAIN TOOL)
**File:** `eWealth-Copilot-Verification-Prompt.md`
**Size:** 12 Sections, 100+ checklist items
**Purpose:** Copy & paste into GitHub Copilot for complete verification

**What it does:**
- Loads all parameters, formulas, and requirements into Copilot context
- Allows you to ask Copilot specific verification questions
- Provides step-by-step verification commands
- Includes sample values for testing
- Lists all known common errors to avoid

**How to use:**
1. Copy entire "COPILOT INSTRUCTION PROMPT" section
2. Paste into GitHub Copilot chat window
3. Ask questions like "Verify Fund_EoY formula" or "List missing output fields"
4. Copilot will check your code against requirements

---

### 2️⃣-7️⃣ CSV Reference Files (QUICK LOOKUPS)

#### File 2: `eWealth-Input-Fields-Reference.csv`
- **31 input fields** with cell references
- Field names, data types, sample values
- Categories (Policy, Premium, Distribution, Investment, Risk, Tax)
- Verification status (✓ Verify, ⚠️ ADD, ⚠️ VERIFY %)
- **Use:** Quick field list, completeness check

#### File 3: `eWealth-Output-Fields-Reference.csv`
- **32 output fields** organized in Part A & Part B
- Part A: 17 columns for 4% scenario, 17 for 8%
- Part B: 32-column detailed breakdown for reporting
- Formulas required for each field
- Sample values for testing
- **Use:** Output structure verification, formula reference

#### File 4: `eWealth-Abbreviations-Reference.csv`
- **10 key abbreviations** (AP, PPT, PT, PAC, ARB, FMC, GST, EMR, ULIF, NSAP)
- Full forms and cell references
- Used in calculations, formula-based status
- **Use:** Understanding formula references, abbreviation lookup

#### File 5: `eWealth-Formulas-Reference.csv`
- **12 critical formulas** (Modal Factor, AP, Mortality, Admin, Fund, etc.)
- Complete formula logic with comments
- Expected returns and sample values
- Verification priority (HIGH, CRITICAL, MEDIUM)
- **Use:** Formula detail reference, testing values

#### File 6: `eWealth-Options-Comparison.csv`
- **2 options** (Platinum, Platinum Plus)
- Feature-by-feature comparison
- ARB inclusion logic
- Death benefit calculation differences
- **Use:** Option branching verification, feature understanding

#### File 7: `eWealth-Parameters-Reference.csv`
- **17 core parameters** with all details
- Source (input or formula), sample values
- Formula-based status (YES/NO), usage area
- **Use:** Parameter completeness check, dependency tracking

---

## 🔍 QUICK START GUIDE

### For Verification with GitHub Copilot:

**Step 1: Load the Prompt**
```
1. Open eWealth-Copilot-Verification-Prompt.md
2. Copy ENTIRE "COPILOT INSTRUCTION PROMPT" section (marked clearly)
3. Paste into GitHub Copilot chat window
```

**Step 2: Ask Verification Questions**
```
Examples:
"Verify Fund_EoY formula for 4% scenario"
"List all missing input fields"
"Check if Mortality_Charges lookup is correct"
"Validate Surrender_Value 5-year lock logic"
"Test Death_Benefit MAX calculation"
```

**Step 3: Review Findings**
```
Copilot will identify:
- Missing fields/columns
- Incorrect formula logic
- Wrong cell references
- Calculation errors
- Missing conditional logic
```

**Step 4: Fix Issues**
```
Copilot can help:
- Write corrected formulas
- Add missing fields
- Implement missing logic
- Test against sample values
```

**Step 5: Verify Completion**
```
Use Section 11 checklist from the prompt:
- Input verification (31 fields)
- Output verification (32 fields)
- Formula verification (12 formulas)
- Sample value verification
```

---

## 📊 KEY INFORMATION BY NEED

### If You Need... → Go To:

**Input Field List**
→ `eWealth-Input-Fields-Reference.csv` (All 31 fields)
→ OR `eWealth-Copilot-Verification-Prompt.md` Section 2

**Output Field List**
→ `eWealth-Output-Fields-Reference.csv` (All 32 fields)
→ OR `eWealth-Copilot-Verification-Prompt.md` Section 3

**Formula Specifications**
→ `eWealth-Formulas-Reference.csv` (12 critical formulas)
→ OR `eWealth-Copilot-Verification-Prompt.md` Section 4

**Sample Calculation Values (for testing)**
→ `eWealth-Copilot-Verification-Prompt.md` Section 8
→ Values for Year 1, 5, 10, 20 at both 4% and 8%

**Option-Specific Logic**
→ `eWealth-Options-Comparison.csv` (Platinum vs Plus)
→ OR `eWealth-Copilot-Verification-Prompt.md` Section 1 & 4

**Common Errors to Avoid**
→ `eWealth-Copilot-Verification-Prompt.md` Section 12

**Parameter Definitions**
→ `eWealth-Parameters-Reference.csv` (17 parameters)
→ OR `eWealth-Copilot-Verification-Prompt.md` Section 1

**Verification Commands**
→ `eWealth-Copilot-Verification-Prompt.md` Section 10

**Complete Verification Checklist**
→ `eWealth-Copilot-Verification-Prompt.md` Section 11

---

## ✅ VERIFICATION CHECKLIST (QUICK VERSION)

Before considering code complete:

**Input Fields (31 Total):**
- [ ] Option field (Platinum/Platinum Plus branching)
- [ ] Age_LifeAssured (B11)
- [ ] Age_PolicyHolder (B9)
- [ ] PolicyTerm (B19)
- [ ] PremiumPaymentTerm (B21)
- [ ] AnnualisedPremium (B27)
- [ ] SumAssured (B31)
- [ ] MaturityAge = B11+B19 (FORMULA, not manual)
- [ ] PremiumInstallment = AP × ModalFactor (FORMULA)
- [ ] All other 22 fields present and used

**Output Fields (32 Total):**
- [ ] 4% Scenario: 17 columns (Year, AP, Mortality, ARB, Charges, GST, Fund, Surrender, Death)
- [ ] 8% Scenario: 17 columns (same structure as 4%)
- [ ] Fund_EoY values: Year 1 (4%): 23048, (8%): 23963 ✓
- [ ] Fund_EoY values: Year 10 (4%): 281519, (8%): 356782 ✓
- [ ] All 20 years populated (Year 1-20)

**Critical Formulas (12 Total):**
- [ ] Modal Factor Lookup (Yearly=1.0, HY=0.5108, Q=0.2582, M=0.0867)
- [ ] Mortality Charges Lookup (Age-based, 357 for Year 1 Age 37 at 4%)
- [ ] Fund_EoY Calculation (CRITICAL - Year 1: 23048 at 4%)
- [ ] Premium Cutoff (Year 11-20 = 0 when PPT=10)
- [ ] Admin Charges Cutoff (Year 11-20 = 0 when PPT=10)
- [ ] Surrender Value Logic (Year 1-4: 0, Year 5+: Fund)
- [ ] Death Benefit (MAX(SA, Fund), not SA+Fund)
- [ ] FMC Deduction (Fund_Before_FMC × 0.001118)
- [ ] ARB Logic (Option 1: 0, Option 2: calculated)
- [ ] Loyalty Addition (Year 6,7,8,15 specific values)
- [ ] Wealth Booster (Year 10,15,20 specific values)
- [ ] Interest Rate Logic (4% and 8% scenarios separate)

**Known Issues to Avoid:**
- ❌ Death Benefit = SA + Fund (should be MAX)
- ❌ Premium continues after PPT (should be 0)
- ❌ Admin charges continue after PPT (should be 0)
- ❌ Surrender available Year 1 (should be Year 5+)
- ❌ Mortality charges static (should progress with age)
- ❌ FMC not deducted (should reduce fund by ~0.1%)
- ❌ Both scenarios using same values (should be independent)
- ❌ ARB present when Option=1 (should be 0)

**Constants to Verify:**
- [ ] PolicyAdminCharge = 1200
- [ ] FMC_Rate = 0.001118
- [ ] InterestRate_4Pct = 0.04
- [ ] InterestRate_8Pct = 0.08
- [ ] GST = 0% (not charged)
- [ ] Modal Factors = 1.0/0.5108/0.2582/0.0867

---

## 🚀 IMPLEMENTATION PHASES

### Phase 1: Load Copilot Context (5 minutes)
- [ ] Open `eWealth-Copilot-Verification-Prompt.md`
- [ ] Copy entire COPILOT INSTRUCTION PROMPT section
- [ ] Paste into GitHub Copilot chat

### Phase 2: Identify Missing Fields (10 minutes)
- [ ] Ask Copilot: "List all missing input fields"
- [ ] Ask Copilot: "List all missing output columns"
- [ ] Review findings against CSV files
- [ ] Add any missing fields

### Phase 3: Verify Formula Logic (20 minutes)
- [ ] Ask Copilot: "Verify Fund_EoY formula"
- [ ] Ask Copilot: "Check Mortality_Charges lookup"
- [ ] Ask Copilot: "Validate Death_Benefit MAX logic"
- [ ] Ask Copilot: "Verify Premium cutoff after PPT"
- [ ] Ask Copilot: "Check Admin charges cutoff"

### Phase 4: Test Sample Values (15 minutes)
- [ ] Ask Copilot: "Compare Year 1 values against samples"
- [ ] Ask Copilot: "Test Year 5 and Year 10 values"
- [ ] Ask Copilot: "Verify both 4% and 8% scenarios"
- [ ] Document any mismatches

### Phase 5: Final Verification (10 minutes)
- [ ] Run through Section 11 checklist
- [ ] Verify no errors from Section 12 (Known Issues)
- [ ] Confirm all 7 critical files available
- [ ] Mark as "Ready for Implementation"

**Total Time:** ~60 minutes for complete verification

---

## 📌 CRITICAL ITEMS (DO NOT MISS)

🔴 **HIGHEST PRIORITY:**
1. **Fund_EoY Calculation** - Must match: Year 1 (4%): 23048, (8%): 23963, Year 10 (4%): 281519, (8%): 356782
2. **Premium Logic** - Must be: Year 1-10 = AP, Year 11-20 = 0
3. **Mortality Charges** - Must lookup from table, progress with age
4. **Death Benefit** - Must use MAX(SA, Fund), NOT SA+Fund (common error)
5. **Option Branching** - ARB must be 0 for Platinum, calculated for Plus

🟠 **HIGH PRIORITY:**
6. **Surrender Value 5-Yr Lock** - Year 1-4: 0, Year 5+: Fund
7. **Admin Charges Cutoff** - Year 1-10: 1200, Year 11-20: 0
8. **FMC Deduction** - ~0.1118% of fund must be deducted
9. **Both Scenarios** - 4% and 8% must be calculated independently
10. **Modal Factor** - Must lookup: Yearly=1.0, HY=0.5108, Q=0.2582, M=0.0867

🟡 **MEDIUM PRIORITY:**
11. Loyalty Addition (Year 6,7,8,15)
12. Wealth Booster (Year 10,15,20)
13. All input fields collected (31 total)
14. All output columns present (32 total)
15. Parameter formulas (MaturityAge, PremiumInstallment as formulas)

---

## 🛠️ USING EACH FILE

### File 1: eWealth-Copilot-Verification-Prompt.md
**When:** Starting verification, need complete instruction set
**How:** Copy prompt section → Paste into Copilot → Ask questions
**Output:** Copilot identifies missing/incorrect items

### File 2: eWealth-Input-Fields-Reference.csv
**When:** Need to verify all input fields present
**How:** Open → Check for all 31 fields → Verify cell references
**Output:** Confirmation of field completeness

### File 3: eWealth-Output-Fields-Reference.csv
**When:** Need to verify output structure
**How:** Open → Check all 32 columns present → Verify formulas
**Output:** Confirmation of output completeness

### File 4: eWealth-Abbreviations-Reference.csv
**When:** Encounter abbreviation in formula, need meaning
**How:** Search abbreviation → Find full form and usage
**Output:** Understanding of formula component

### File 5: eWealth-Formulas-Reference.csv
**When:** Need formula detail, sample values, or verification values
**How:** Find formula → Review logic → Check sample values
**Output:** Formula validation and testing values

### File 6: eWealth-Options-Comparison.csv
**When:** Need to understand Platinum vs Platinum Plus differences
**How:** Open → Compare features → Verify branching logic
**Output:** Option-specific requirements understood

### File 7: eWealth-Parameters-Reference.csv
**When:** Need to verify all parameters present and used correctly
**How:** Open → Check each parameter → Verify usage
**Output:** Parameter completeness and correctness confirmed

---

## 📞 QUICK REFERENCE ABBREVIATIONS

| Abbr | Meaning | Used In |
|------|---------|---------|
| AP | Annualized Premium | Fund calc every year |
| PPT | Premium Payment Term | Premium cutoff logic |
| PT | Policy Term | Year loop (1 to PT) |
| SA | Sum Assured | Death Benefit calc |
| PAC | Premium Allocation Charges | Fund deduction (0 for ULIF) |
| ARB | Additional Risk Benefit | Platinum Plus only |
| EMR | Extra Mortality Rate | Extra charges |
| FMC | Fund Management Charge | Fund EoY calculation |
| GST | Goods & Services Tax | 0% for ULIF |
| ULIF | Unit-Linked Insurance Fund | Product type |
| OptCode | Option Code | 1=Platinum, 2=Plus |
| Freq | Premium Frequency | Modal factor lookup |
| MA | Maturity Age | Calculated (Age+PT) |
| DB | Death Benefit | MAX(SA, Fund) |
| SV | Surrender Value | Year 5+, fund value |
| MF | Modal Factor | Premium frequency factor |

---

## 🎓 LEARNING PATH

**New to e-Wealth Royale?**

1. **Read First:** eWealth-Copilot-Verification-Prompt.md, Section 1 (Parameters)
2. **Understand:** eWealth-Options-Comparison.csv (Platinum vs Plus)
3. **Learn Formulas:** eWealth-Formulas-Reference.csv (12 key formulas)
4. **See Complete Output:** eWealth-Output-Fields-Reference.csv
5. **Load Copilot:** Full prompt from prompt file
6. **Test:** Use sample values from Section 8 of prompt

**Experienced with ULIF?**

1. **Quick Load:** Just load the Copilot prompt
2. **Ask Specific Questions:** "Verify Fund_EoY formula"
3. **Cross-Check:** Use CSV files for quick lookups
4. **Test Values:** Year 1 = 23048 (4%), 23963 (8%)

---

## ✅ BEFORE YOU START VERIFICATION

**Checklist:**
- [ ] Have all 7 files downloaded/available
- [ ] Have GitHub Copilot open and ready
- [ ] Have current code/implementation available
- [ ] Have spreadsheet (if Excel-based) open
- [ ] Have 60 minutes available for full verification
- [ ] Have sample calculation values ready (in prompt)
- [ ] Have test scenarios ready (Platinum + Plus)

**Documentation Checklist:**
- [ ] `eWealth-Copilot-Verification-Prompt.md` ✓
- [ ] `eWealth-Input-Fields-Reference.csv` ✓
- [ ] `eWealth-Output-Fields-Reference.csv` ✓
- [ ] `eWealth-Formulas-Reference.csv` ✓
- [ ] `eWealth-Abbreviations-Reference.csv` ✓
- [ ] `eWealth-Options-Comparison.csv` ✓
- [ ] `eWealth-Parameters-Reference.csv` ✓

---

## 🔍 VERIFICATION SUCCESS INDICATORS

When verification is complete, you'll have:

✅ **Complete Field List**
- All 31 input fields identified and mapped
- All 32 output fields identified and mapped
- Cell references verified
- Data types confirmed

✅ **Correct Formulas**
- Fund_EoY matches sample values exactly
- Mortality charges lookup working correctly
- Premium logic stops after PPT
- Admin charges stop after PPT
- Death benefit uses MAX logic
- FMC deduction correct (~0.1%)

✅ **Both Scenarios Working**
- 4% scenario values correct
- 8% scenario values correct
- Both independent (not shared values)

✅ **Logic Verified**
- Option branching correct (Platinum vs Plus)
- Surrender value 5-year lock working
- Age progression in mortality correct
- Modal factors appropriate

✅ **No Known Errors**
- Death Benefit is MAX, not addition
- Premium doesn't continue after PPT
- Surrender not available before Year 5
- Mortality charges not static
- FMC is deducted
- ARB correct for each option

✅ **Ready for Implementation**
- All checklist items complete
- All sample values match
- Code tested against samples
- Documentation approved
- Team has all references
- Next phase can begin

---

## 📝 FINAL NOTES

**This documentation package includes:**
- Complete GitHub Copilot instruction set (ready to copy/paste)
- 6 CSV reference tables (for quick lookups)
- 100+ checklist items for thorough verification
- 12 critical formula specifications
- Sample calculation values for testing
- Known issues and error prevention guide
- Complete parameter and field definitions

**What you can do with this:**
1. ✅ Verify code completeness (all fields present)
2. ✅ Verify formula correctness (logic and values)
3. ✅ Identify missing functionality
4. ✅ Test against real calculation values
5. ✅ Fix errors with Copilot assistance
6. ✅ Document findings and corrections
7. ✅ Approve code for production

**Time to complete verification:** 60 minutes

---

**Document Generated:** 2026-03-18  
**Product:** SUD Life e-Wealth Royale (ULIF)  
**Status:** ✅ COMPLETE & READY FOR GITHUB COPILOT VERIFICATION  

**Next Step:** Copy the Copilot Prompt and start verification!

---

*This package was created with detailed analysis of the e-Wealth Royale ULIF product structure. All formulas, parameters, and sample values have been extracted from the actual product specification. Use with GitHub Copilot for efficient code verification and correction.*