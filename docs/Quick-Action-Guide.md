# ⚡ QUICK ACTION GUIDE
## e-Wealth Royale GitHub Copilot Verification (5-Minute Start)

---

## 🚀 START HERE (RIGHT NOW)

### What You Have:
- ✅ GitHub Copilot Prompt (copy/paste ready)
- ✅ 6 CSV Reference Files (for lookups)
- ✅ Complete Specification (all formulas & fields)

### What You Need to Do (in order):

---

## STEP 1: Open the Main Tool (2 minutes)

**File to Open:**
→ `eWealth-Copilot-Verification-Prompt.md`

**What to Copy:**
→ Find section: "## COPILOT INSTRUCTION PROMPT"
→ Copy EVERYTHING between the three backticks (```...```)
→ This is your complete instruction set for Copilot

**Where to Paste:**
→ GitHub Copilot chat window
→ New conversation

**What Copilot Will Do:**
→ Load all product specs into context
→ Ready to verify your code

---

## STEP 2: Ask Copilot Key Questions (3 minutes)

After pasting the prompt, ask ONE question at a time:

### QUESTION 1 (Most Important):
```
"Verify the Fund_EoY formula for both 4% and 8% scenarios.
Year 1 should be: 4%=23048, 8%=23963
Year 10 should be: 4%=281519, 8%=356782
Are these values matching in the code?"
```

### QUESTION 2 (Death Benefit):
```
"Is Death_Benefit calculated as MAX(Sum_Assured, Fund_EoY)?
NOT as Sum_Assured + Fund_EoY (which is a common error)"
```

### QUESTION 3 (Premium Logic):
```
"Does the code deduct premium payments?
Years 1-10: Premium = 24000
Years 11-20: Premium = 0 (PPT ends at Year 10)
Is this implemented correctly?"
```

### QUESTION 4 (Missing Fields):
```
"List all INPUT fields that are missing from the code.
Reference: There should be 31 input fields total.
Use eWealth-Input-Fields-Reference.csv for comparison."
```

### QUESTION 5 (Missing Outputs):
```
"List all OUTPUT columns that are missing.
Reference: Should have 32 columns total (17 for 4%, 17 for 8%).
Use eWealth-Output-Fields-Reference.csv for comparison."
```

---

## STEP 3: Review Copilot Findings (Ongoing)

Copilot will tell you:
- ❌ What's missing
- ❌ What's incorrect
- ❌ What formula logic is wrong
- ✅ What's correct

**Keep a notes document with:**
- [ ] Missing fields identified
- [ ] Incorrect formulas found
- [ ] Logic errors discovered
- [ ] Action items to fix

---

## STEP 4: Fix Issues with Copilot Help (Ongoing)

**Ask Copilot:**
```
"Write the correct formula for [specific field/calculation]
to calculate [what it should calculate]
The expected result is [sample value]"
```

**Example:**
```
"Write the correct formula for Fund_EoY calculation
to add premium, deduct charges, apply interest, and deduct FMC
The expected result for Year 1 at 4% is 23048"
```

Copilot can:
- Write correct formulas
- Explain the logic
- Show step-by-step calculation
- Test against sample values

---

## STEP 5: Cross-Check with CSV Files (Anytime)

When you need quick reference:

| Need | CSV File |
|------|----------|
| All input fields & cell refs | eWealth-Input-Fields-Reference.csv |
| All output fields & formulas | eWealth-Output-Fields-Reference.csv |
| Formula details & samples | eWealth-Formulas-Reference.csv |
| Quick abbreviation lookup | eWealth-Abbreviations-Reference.csv |
| Parameter definitions | eWealth-Parameters-Reference.csv |
| Option comparison | eWealth-Options-Comparison.csv |

---

## 🎯 CRITICAL ITEMS (DO NOT SKIP)

Must verify these 5 items:

### ✅ Item 1: Fund_EoY Formula (MOST CRITICAL)
- [ ] Year 1 (4%): 23,048
- [ ] Year 1 (8%): 23,963
- [ ] Year 10 (4%): 281,519
- [ ] Year 10 (8%): 356,782

### ✅ Item 2: Death Benefit Uses MAX (NOT +)
- [ ] Formula: MAX(Sum_Assured, Fund_EoY)
- [ ] NOT: Sum_Assured + Fund_EoY
- [ ] Year 1 result: 240,000
- [ ] Year 10 result: 281,519 (or 356,782 for 8%)

### ✅ Item 3: Premium Stops After Year 10 (PPT=10)
- [ ] Year 1-10: Deduct AP = 24,000
- [ ] Year 11-20: Deduct AP = 0
- [ ] Year 11 Fund should grow without premium input

### ✅ Item 4: Admin Charges Stop After Year 10
- [ ] Year 1-10: Deduct 1,200
- [ ] Year 11-20: Deduct 0
- [ ] Fund grows faster in Years 11-20

### ✅ Item 5: Both Scenarios (4% & 8%) are Independent
- [ ] 4% scenario values ≠ 8% scenario values
- [ ] Must have separate calculations
- [ ] Must produce different results

---

## ❌ ERRORS TO PREVENT

**Error #1: Death = SA + Fund (WRONG)**
```
❌ Wrong: Year 10 DB = 240,000 + 281,519 = 521,519
✅ Right: Year 10 DB = MAX(240,000, 281,519) = 281,519
```

**Error #2: Premium continues after PPT (WRONG)**
```
❌ Wrong: Year 11-20 still deduct 24,000
✅ Right: Year 11-20 deduct 0
```

**Error #3: Surrender available Year 1 (WRONG)**
```
❌ Wrong: Year 1 SV = 23,048
✅ Right: Year 1 SV = 0, Year 5+ SV = Fund
```

**Error #4: Static mortality charges (WRONG)**
```
❌ Wrong: All years show 357
✅ Right: Year 1=357, Year 2=356, Year 3=354...
```

**Error #5: Both rates using same values (WRONG)**
```
❌ Wrong: 4% and 8% columns have identical values
✅ Right: 4%≠8%, values should be significantly different
```

---

## 📋 MINIMAL VERIFICATION CHECKLIST

**Input Fields (Check These):**
- [ ] Option (Platinum/Platinum Plus)
- [ ] Age_LifeAssured (B11)
- [ ] PolicyTerm (B19)
- [ ] PremiumPaymentTerm (B21) = 10
- [ ] AnnualisedPremium (B27) = 24000
- [ ] SumAssured (B31) = 240000
- [ ] MaturityAge = B11+B19 (FORMULA)
- [ ] All 31 fields present (check CSV file)

**Output Columns (Check These):**
- [ ] Years 1-20 populated
- [ ] 4% Scenario: 17 columns (B through I)
- [ ] 8% Scenario: 17 columns (J through P)
- [ ] Fund_EoY values correct (see above)
- [ ] Death_Benefit = MAX logic
- [ ] Surrender value: 0 for Yr1-4, Fund for Yr5+
- [ ] Premium: 24000 for Yr1-10, 0 for Yr11-20
- [ ] Admin: 1200 for Yr1-10, 0 for Yr11-20

**Formulas (Check These):**
- [ ] Modal Factor Lookup (Yearly=1.0, etc.)
- [ ] Mortality Charges Lookup (Age-based, 357 for Yr1)
- [ ] Fund_EoY Calculation (Year 1: 23048 at 4%)
- [ ] Premium cutoff after PPT
- [ ] Admin charges cutoff after PPT
- [ ] FMC deduction (~0.1%)
- [ ] MAX logic for Death Benefit
- [ ] Surrender 5-year lock

**Sample Values (Test These):**
- [ ] Year 1 Fund (4%): 23,048
- [ ] Year 1 Fund (8%): 23,963
- [ ] Year 5 Fund (4%): 121,638
- [ ] Year 10 Fund (4%): 281,519
- [ ] Year 10 Fund (8%): 356,782

---

## 📞 IF YOU GET STUCK

### Problem: "Copilot doesn't understand"
**Solution:** 
- Load the prompt again (fresh conversation)
- Ask simpler, one-question-at-a-time
- Reference sample values directly

### Problem: "Formula is too complex"
**Solution:**
- Break it into steps
- Ask for Step 1, Step 2, Step 3 separately
- Test each step independently

### Problem: "Values don't match samples"
**Solution:**
- List actual value vs expected value
- Ask Copilot to identify the difference
- Ask where in formula the error might be

### Problem: "Don't know if field is missing"
**Solution:**
- Open eWealth-Input-Fields-Reference.csv
- Check if field is listed
- Check if it's in your code
- Ask Copilot to confirm

### Problem: "Need more detail on a formula"
**Solution:**
- Open eWealth-Formulas-Reference.csv
- Find the formula
- Read the "Returns" column for expected value
- Ask Copilot to implement that exact logic

---

## ⏱️ TIME BREAKDOWN

| Step | Task | Time |
|------|------|------|
| 1 | Load Copilot Prompt | 2 min |
| 2 | Ask 5 key questions | 5 min |
| 3 | Review findings | 5 min |
| 4 | Fix identified issues | 20 min |
| 5 | Test against samples | 10 min |
| 6 | Final verification | 10 min |
| **Total** | **Complete verification** | **52 min** |

---

## 🎓 HELPFUL TIPS

**Tip 1: Copy/Paste is Your Friend**
- Copy prompt sections directly into Copilot
- Copy sample values from CSV files
- Paste Copilot responses into your notes

**Tip 2: Use Section References**
- Prompt has 12 sections, clearly marked
- Tell Copilot "See Section 8 for sample values"
- Tell Copilot "Section 4 has formula details"

**Tip 3: Test One Scenario at a Time**
- First verify 4% scenario completely
- Then verify 8% scenario
- Then verify both are independent

**Tip 4: Use the CSV Files as Lookup**
- When Copilot mentions a formula, find it in CSV
- When unsure about abbreviation, check CSV
- When Copilot gives a value, verify against CSV

**Tip 5: Ask Copilot to Show Its Work**
- Ask "Show step-by-step calculation"
- Ask "What happens in Year 5?"
- Ask "Why does this value change?"

---

## ✅ SUCCESS CHECKLIST

Mark these off as you go:

```
⏹️ BEFORE YOU START
  [ ] All 7 files downloaded
  [ ] GitHub Copilot open
  [ ] Current code/implementation ready
  [ ] 1 hour available

⏹️ STEP 1: LOAD PROMPT
  [ ] Opened eWealth-Copilot-Verification-Prompt.md
  [ ] Copied COPILOT INSTRUCTION PROMPT section
  [ ] Pasted into Copilot chat
  [ ] Copilot responded successfully

⏹️ STEP 2: ASK KEY QUESTIONS
  [ ] Asked Question 1 (Fund_EoY)
  [ ] Asked Question 2 (Death Benefit MAX)
  [ ] Asked Question 3 (Premium Logic)
  [ ] Asked Question 4 (Missing Inputs)
  [ ] Asked Question 5 (Missing Outputs)

⏹️ STEP 3: IDENTIFY ISSUES
  [ ] Found missing fields: ___________
  [ ] Found wrong formulas: ___________
  [ ] Found calculation errors: __________
  [ ] Total issues: _____ items

⏹️ STEP 4: FIX ISSUES
  [ ] Fixed Fund_EoY formula
  [ ] Fixed Death Benefit logic
  [ ] Fixed Premium cutoff
  [ ] Fixed Admin charges cutoff
  [ ] Added missing fields
  [ ] Added missing columns

⏹️ STEP 5: TEST AGAINST SAMPLES
  [ ] Year 1 (4%): Fund = 23,048 ✓
  [ ] Year 1 (8%): Fund = 23,963 ✓
  [ ] Year 10 (4%): Fund = 281,519 ✓
  [ ] Year 10 (8%): Fund = 356,782 ✓
  [ ] Death Benefit logic correct ✓

⏹️ FINAL VERIFICATION
  [ ] All 31 input fields present
  [ ] All 32 output columns present
  [ ] All sample values matching
  [ ] No errors from error list
  [ ] Both scenarios independent
  [ ] Code ready for implementation ✓
```

---

## 📞 QUICK CONTACTS

**If you need the:**

**→ Complete formula spec**
Use: `eWealth-Formulas-Reference.csv`

**→ All input fields list**
Use: `eWealth-Input-Fields-Reference.csv`

**→ All output fields list**
Use: `eWealth-Output-Fields-Reference.csv`

**→ Abbreviation meaning**
Use: `eWealth-Abbreviations-Reference.csv`

**→ Parameter definition**
Use: `eWealth-Parameters-Reference.csv`

**→ Option differences**
Use: `eWealth-Options-Comparison.csv`

**→ Complete documentation index**
Use: `eWealth-Documentation-Index.md`

---

## 🎯 YOUR NEXT ACTION RIGHT NOW

1. ✅ Open: `eWealth-Copilot-Verification-Prompt.md`
2. ✅ Find: "COPILOT INSTRUCTION PROMPT" section
3. ✅ Copy: Everything between the three backticks
4. ✅ Open: GitHub Copilot chat
5. ✅ Paste: The prompt
6. ✅ Ask: "Verify Fund_EoY formula..."
7. ✅ Start verification

**Time to start:** RIGHT NOW
**Expected to complete:** 60 minutes
**Result:** Code verified and ready for implementation

---

**Status:** ✅ Ready to Start  
**Files needed:** All 7 files (you have them)  
**Tools needed:** GitHub Copilot (open it now)  
**Time needed:** 60 minutes  

**Let's Go! 🚀**