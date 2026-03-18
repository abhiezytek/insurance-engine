# Century Income Review Guide

## Product nature
- Traditional / Endowment style product
- Non-linked, non-participating savings plan
- Options: Immediate Income, Deferred Income, Twin Income

## Required checks

### Inputs
Validate manual input and fetched field coverage for:
- policy number
- as on date
- commencement date
- risk commencement date
- age / DOB
- gender
- option
- premium payment term
- policy term
- premium frequency
- annualized premium
- installment premium
- number of premiums paid
- total premiums paid
- policy status
- survival benefits paid till date
- income paid till date
- sum assured / maturity basis

### Lookup dependencies
- Annexure 1: GMB factors
- Annexure 2: Guaranteed Surrender Value factors
- Annexure 3: Special Surrender Value factors

### Formula checks
Validate:
- option mapping
- modal factors
- GMB formula
- GSV formula
- SSV formula
- Surrender Value = max(GSV, SSV)
- paid-up factor
- in-force vs reduced paid-up benefits
- death benefit
- maturity benefit
- survival benefit pattern by option
- revival / lapsed / paid-up status handling

## Expected outputs
- Surrender Value as on date
- Maturity Value as on date
- Maturity Value on maturity if all future premiums are paid
- Traditional format downloadable output
