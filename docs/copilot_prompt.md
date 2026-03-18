# GitHub Copilot Prompt for PrecisionPro YPYG Review

You are reviewing an existing PrecisionPro codebase that calculates policy values and generates downloadable output documents for:
1) YPYG - Century Income (Traditional / Endowment style product)
2) YPYG - e-Wealth Royale (ULIP product)

Your job is NOT to create assumptions. Your job is to verify, compare, and correct the existing implementation against the business intent, BI/FU files, annexure tables, and output formats.

## Objective

Review the existing code and verify:
- input model completeness
- formula correctness
- downloadable output correctness
- as-on-date engine correctness
- module/menu/admin structure changes

## Products

### 1. Century Income (Traditional / Endowment)
- Non-linked, non-participating savings product.
- 3 options: Immediate Income, Deferred Income, Twin Income.
- Surrender Value = higher of GSV or SSV.
- GMB factors from Annexure 1.
- GSV factors from Annexure 2.
- SSV factors from Annexure 3.
- Reduced paid-up rules, death benefit, maturity benefit, and survival benefit depend on policy status and option.

### 2. e-Wealth Royale (ULIP)
- Unit-linked non-participating life insurance plan.
- 2 options: Platinum and Platinum Plus.
- Logic includes fund value, mortality charge, additional risk benefit charge, PAC, FMC, discontinuance/surrender rules, loyalty additions, wealth boosters, return of charges, death benefit, maturity value, paid-up/discontinuance/revival.

## Required YPYG modes

### Mode 1: Policy Number Mode
User enters only policy number.
System fetches policy details from core and calculates:
- surrender value as on date
- maturity value as on date
- maturity value on maturity assuming all future premiums are paid
- key policy summary fields

### Mode 2: Manual Input Mode
User enters required fields manually.
Must support calculation of:
- surrender value as on date
- maturity value as on date
- maturity value on maturity if all future premiums are paid

Important: Risk Commencement Date and Number of Years Premium Paid / Number of Premiums Paid are mandatory for manual input.

## Mandatory review areas

### Century Income
Validate:
- option mapping
- modal factors
- GMB lookup
- GSV formula and lookup
- SSV formula and lookup
- surrender value = max(GSV, SSV)
- survival benefit logic by option
- death benefit
- maturity benefit
- reduced paid-up / lapsed / revival logic

### e-Wealth Royale
Validate:
- Platinum vs Platinum Plus option logic
- death benefit highest-of logic
- risk commencement for minor life
- mortality charge
- additional risk benefit charge
- PAC only for first 10 policy years
- FMC by fund
- loyalty additions from end of year 6 to PPT
- wealth booster every 5th year from year 10
- RoPAC at year 10
- RoMC at maturity
- RoARBC at maturity where applicable
- lock-in / discontinuance / surrender logic
- paid-up / revival handling

## YPYG outputs to validate
For both products, confirm output documents support:
- Surrender Value as on Date
- Maturity Value as on Date
- Maturity Value on Maturity if all future premiums are paid

Also validate downloadable formats:
- YPYG ULIP Format
- YPYG Traditional Format

## PrecisionPro structure changes
Top-level modules required:
- Benefit Illustration
- YPYG
- Audit
- Admin

Audit should be simplified so that both Payout and Addition/Bonus have only:
- Dashboard
- Single Policy
- Bulk Upload

Admin must support:
- create/deactivate user
- module access control
- product access control
- new product onboarding
- formula/rule configuration
- lookup maintenance
- template mapping
- audit trail

## Deliverables expected from your review
Return findings in sections:
1. Executive Summary
2. Century Income - Input and Formula Review
3. e-Wealth Royale - Input and Formula Review
4. YPYG Output Template Review
5. PrecisionPro Module / Audit / Admin Changes
6. Prioritized Fix List
7. Suggested Data Models / DTOs / API Contracts
8. Suggested corrected formulas / pseudocode
9. Open questions requiring business confirmation

Be explicit, implementation-focused, and do not assume unsupported rules.
