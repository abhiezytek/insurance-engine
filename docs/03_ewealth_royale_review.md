# e-Wealth Royale Review Guide

## Product nature
- ULIP product
- Options: Platinum, Platinum Plus

## Required checks

### Inputs
Validate manual input and fetched field coverage for:
- policy number
- as on date
- commencement date
- risk commencement date
- DOB / age of life assured
- DOB / age of policyholder
- gender
- option
- policy term
- premium payment term
- premium frequency
- annualized premium
- installment premium
- sum assured
- number of premiums paid
- total premiums paid
- policy status
- current fund value / fund-wise values
- investment strategy
- fund allocation
- partial withdrawals in last 24 months
- charge history if needed

### Formula checks
Validate:
- death benefit highest-of logic
- minimum death benefit = 105% of premiums paid
- risk commencement date for minor life
- mortality charge
- additional risk benefit charge for Platinum Plus
- PAC only first 10 policy years
- FMC by selected fund
- loyalty additions from year 6 to PPT
- wealth boosters every 5th year from year 10
- RoPAC at end of year 10
- RoMC at maturity
- RoARBC at maturity if eligible
- surrender within lock-in
- surrender after lock-in
- paid-up logic
- discontinuance logic
- revival logic

## Expected outputs
- Surrender Value as on date
- Maturity Value as on date
- Maturity Value on maturity if all future premiums are paid
- ULIP format downloadable output
