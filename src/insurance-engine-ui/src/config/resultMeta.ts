export const TBV_META = {
  traditional: [
    { label: 'Survival Benefit', currentKey: 'currentSurvivalBenefit', maturityKey: 'maturitySurvivalBenefit' },
    { label: 'Maturity Benefit', currentKey: 'currentMaturityBenefit', maturityKey: 'maturityMaturityBenefit' },
    { label: 'Death Benefit', currentKey: 'currentDeathBenefit', maturityKey: 'maturityDeathBenefit' },
  ] as const,
  ulip: [
    { label: 'Fund Value (@4%)', currentKey: 'currentFundValue4', maturityKey: 'maturityFundValue4' },
    { label: 'Fund Value (@8%)', currentKey: 'currentFundValue8', maturityKey: 'maturityFundValue8' },
    { label: 'Maturity Benefit (@4%)', currentKey: 'currentFundValue4', maturityKey: 'maturityBenefit4' },
    { label: 'Maturity Benefit (@8%)', currentKey: 'currentFundValue8', maturityKey: 'maturityBenefit8' },
    { label: 'Death Benefit (@4%)', currentKey: 'currentDeathBenefit4', maturityKey: 'maturityDeathBenefit4' },
    { label: 'Death Benefit (@8%)', currentKey: 'currentDeathBenefit8', maturityKey: 'maturityDeathBenefit8' },
  ] as const,
};

export const YPYG_RESULT_META = {
  Traditional: {
    tableTitle: 'Yearly Benefit Table',
    pdfTemplate: 'traditional',
    yearlyHeaders: ['Yr', 'Annual Premium (Rs.)', 'Total Paid (Rs.)', 'Guaranteed Income (Rs.)', 'Loyalty Income (Rs.)', 'Total Income (Rs.)', 'Surrender Value (Rs.)', 'Death Benefit (Rs.)', 'Maturity Benefit (Rs.)'],
    tbvRows: TBV_META.traditional,
  },
  ULIP: {
    tableTitle: 'ULIP Fund Projection Table',
    pdfTemplate: 'ulip',
    yearlyHeaders: ['Yr', 'Age', 'Annual Prem.', 'Invested', 'MC', 'PC', 'FV @4%', 'DB @4%', 'SV @4%', 'FV @8%', 'DB @8%', 'SV @8%'],
    tbvRows: TBV_META.ulip,
  },
} as const;
