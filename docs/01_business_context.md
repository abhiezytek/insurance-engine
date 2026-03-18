# PrecisionPro YPYG Business Context

## Scope
This repository review is for PrecisionPro features covering:
- Benefit Illustration
- YPYG
- Audit
- Admin

The YPYG scope covers:
1. Century Income as Traditional / Endowment style product
2. e-Wealth Royale as ULIP product

## Business objective
The solution must support two YPYG processing modes:
- Policy Number mode using core integration
- Manual Input mode using user-entered fields

Both modes must calculate:
- Surrender Value as on date
- Maturity Value as on date
- Maturity Value at maturity assuming all future premiums are paid

## Critical business requirements
- Risk Commencement Date is required.
- Number of Years Premium Paid / Number of Premiums Paid is required.
- Output documents must match attached YPYG ULIP and Traditional formats.
- Formula logic must align to BI, File & Use, and annexure tables.
- No business-critical values should be hardcoded if lookup-based.

## UI requirements
Modules required in PrecisionPro:
- Benefit Illustration
- YPYG
- Audit
- Admin

Audit simplification required:
- Payout -> Dashboard, Single Policy, Bulk Upload
- Addition/Bonus -> Dashboard, Single Policy, Bulk Upload

Dashboard issue:
- Current Modules section appears non-clickable and must route to working pages.

## Admin requirements
Admin must support:
- create user
- activate/deactivate user
- assign module access
- assign product access
- manage subscriptions
- onboard new products
- configure new formulas/rules
- maintain lookup tables
- map templates
- preserve audit trail of changes
