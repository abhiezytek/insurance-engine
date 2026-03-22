export type ProductCode = 'CENTURY_INCOME' | 'EWEALTH_ROYALE';

export type Gender = 'Male' | 'Female' | 'Transgender';

export type PremiumFrequency = 'Yearly' | 'Half-Yearly' | 'Quarterly' | 'Monthly';

export type SalesChannel =
  | 'Corporate Agency'
  | 'Agency'
  | 'Broker'
  | 'Direct Marketing'
  | 'Online'
  | 'Web Aggregator'
  | 'Insurance Marketing Firm';

export const MODAL_FACTORS: Record<PremiumFrequency, number> = {
  Yearly: 1,
  'Half-Yearly': 0.5108,
  Quarterly: 0.2582,
  Monthly: 0.0867,
};

export type CenturyIncomePpt = 7 | 10 | 12 | null;
export type CenturyIncomePt = 15 | 20 | 25 | null;

export interface CenturyIncomeForm {
  product: 'CENTURY_INCOME';
  option: 'Immediate' | 'Deferred' | 'Twin Income';
  isProposerDifferent: boolean;
  lifeAssuredName: string;
  proposerName?: string;
  lifeAssuredDob: string | null;
  proposerDob?: string | null;
  lifeAssuredAge?: number | null;
  proposerAge?: number | null;
  lifeAssuredGender: Gender | null;
  proposerGender?: Gender | null;
  premium: number | null;
  premiumFrequency: PremiumFrequency | null;
  annualisedPremium?: number | null;
  sumAssured?: number | null;
  ppt: CenturyIncomePpt;
  pt: CenturyIncomePt;
  standardAgeProof: boolean | null;
  salesChannel: SalesChannel | null;
  staffPolicy: boolean | null;
}

export type PptType = 'Single' | 'Limited' | 'Regular' | null;

export type InvestmentStrategy =
  | 'Self-Managed Investment Strategy'
  | 'Age-Based Strategy'
  | 'System Managed'
  | null;

export interface EwealthRoyaleForm {
  product: 'EWEALTH_ROYALE';
  option: string | null;
  isProposerDifferent: boolean;
  lifeAssuredName: string;
  proposerName?: string;
  lifeAssuredDob: string | null;
  proposerDob?: string | null;
  lifeAssuredAge?: number | null;
  proposerAge?: number | null;
  lifeAssuredGender: Gender | null;
  proposerGender?: Gender | null;
  policyEffectiveDate: string | null;
  premium: number | null;
  premiumFrequency: PremiumFrequency | null;
  annualisedPremium?: number | null;
  pptType: PptType;
  pptYears: number | null;
  pt: number | null;
  sumAssured?: number | null;
  investmentStrategy: InvestmentStrategy;
  fundOption?: string | null;
  standardAgeProof: boolean | null;
  salesChannel: SalesChannel | null;
  staffPolicy: boolean | null;
}

export const SELF_MANAGED_FUNDS = [
  'Blue Chip Equity Fund',
  'Growth Plus Fund',
  'Balance Plus Fund',
  'Mid Cap Fund',
  'Dynamic Fund',
  'Money Market Fund',
  'Gilt Fund',
  'Income Fund',
  'New India Leaders Fund',
  'Viksit Bharat Fund',
  'SUD Life Midcap Momentum Index Fund',
  'SUD Life Nifty Alpha 50 Index Fund',
] as const;

export function deriveAnnualisedPremium(premium: number, frequency: PremiumFrequency): number {
  const factor = MODAL_FACTORS[frequency];
  return Math.round(premium / factor);
}

export function calculateAge(dob: string | null, asOf: Date = new Date()): number | null {
  if (!dob) return null;
  const birth = new Date(dob);
  if (Number.isNaN(birth.getTime())) return null;
  let age = asOf.getFullYear() - birth.getFullYear();
  const hasNotHadBirthday =
    asOf.getMonth() < birth.getMonth() ||
    (asOf.getMonth() === birth.getMonth() && asOf.getDate() < birth.getDate());
  if (hasNotHadBirthday) age -= 1;
  return age;
}

export function getCenturyIncomePtOptions(ppt: CenturyIncomePpt): number[] {
  if (ppt === 7) return [15, 20];
  if (ppt === 10) return [20, 25];
  if (ppt === 12) return [25];
  return [];
}

export function deriveCenturyIncomeValues(form: CenturyIncomeForm) {
  const annualisedPremium =
    form.premium && form.premiumFrequency
      ? deriveAnnualisedPremium(form.premium, form.premiumFrequency)
      : null;

  const sumAssured = annualisedPremium ? annualisedPremium * 10 : null;

  return { annualisedPremium, sumAssured };
}

export function validateCenturyIncome(form: CenturyIncomeForm): string[] {
  const errors: string[] = [];

  if (!form.product) errors.push('Product is required');
  if (!form.option) errors.push('Option is required');
  if (!form.lifeAssuredDob) errors.push('Life Assured DOB is required');
  if (!form.lifeAssuredGender) errors.push('Life Assured Gender is required');
  if (!form.premiumFrequency) errors.push('Premium Frequency is required');
  if (!form.premium) errors.push('Premium is required');
  if (!form.ppt) errors.push('Premium Payment Term is required');
  if (!form.pt) errors.push('Policy Term is required');

  const derived = deriveCenturyIncomeValues(form);
  if (derived.annualisedPremium && derived.annualisedPremium < 50000) {
    errors.push('Minimum annualised premium is 50,000');
  }

  if (form.ppt && form.pt && !getCenturyIncomePtOptions(form.ppt).includes(form.pt)) {
    errors.push('Invalid Policy Term for selected Premium Payment Term');
  }

  return errors;
}

export function getUlipPptYearOptions(pptType: PptType): number[] {
  if (pptType === 'Single') return [1];
  if (pptType === 'Limited') return [5, 7, 10];
  if (pptType === 'Regular') return []; // derived from PT
  return [];
}

export function range(min: number, max: number): number[] {
  const values: number[] = [];
  for (let i = min; i <= max; i += 1) values.push(i);
  return values;
}

export function getUlipPtOptions(pptType: PptType, pptYears: number | null): number[] {
  if (pptType === 'Single') return range(10, 40);
  if (pptType === 'Limited' && (pptYears === 5 || pptYears === 7)) return range(10, 40);
  if (pptType === 'Limited' && pptYears === 10) return range(15, 40);
  if (pptType === 'Regular') return range(10, 40);
  return [];
}

export function deriveUlipPptYears(
  pptType: PptType,
  pt: number | null,
  selectedLimited?: number | null,
): number | null {
  if (pptType === 'Single') return 1;
  if (pptType === 'Limited') return selectedLimited ?? null;
  if (pptType === 'Regular') return pt;
  return null;
}

export function deriveUlipValues(form: EwealthRoyaleForm) {
  const annualisedPremium =
    form.premium && form.premiumFrequency
      ? deriveAnnualisedPremium(form.premium, form.premiumFrequency)
      : null;

  const sumAssured =
    form.pptYears === 1
      ? (form.premium ?? 0) * 1.25
      : annualisedPremium
        ? annualisedPremium * 10
        : null;

  return { annualisedPremium, sumAssured };
}

export function shouldShowFundOption(strategy: InvestmentStrategy): boolean {
  return strategy === 'Self-Managed Investment Strategy';
}

export function onInvestmentStrategyChange(strategy: InvestmentStrategy, form: EwealthRoyaleForm) {
  const nextForm = { ...form, investmentStrategy: strategy };
  if (!shouldShowFundOption(strategy)) {
    nextForm.fundOption = null;
  }
  return nextForm;
}

export function validateUlip(form: EwealthRoyaleForm): string[] {
  const errors: string[] = [];

  if (!form.product) errors.push('Product is required');
  if (!form.option) errors.push('Option is required');
  if (!form.lifeAssuredDob) errors.push('Life Assured DOB is required');
  if (!form.lifeAssuredGender) errors.push('Life Assured Gender is required');
  if (!form.policyEffectiveDate) errors.push('Policy Effective Date is required');
  if (!form.premiumFrequency) errors.push('Premium Frequency is required');
  if (!form.premium) errors.push('Premium is required');
  if (!form.pptType) errors.push('PPT Type is required');
  if (!form.pt) errors.push('Policy Term is required');

  const derived = deriveUlipValues(form);

  if (form.pptYears === 1) {
    if ((form.premium ?? 0) < 250000) {
      errors.push('Minimum single premium is 250,000');
    }
  } else if (derived.annualisedPremium && form.premiumFrequency) {
    const minByFreq: Record<PremiumFrequency, number> = {
      Yearly: 50000,
      'Half-Yearly': 30000,
      Quarterly: 15000,
      Monthly: 5000,
    };
    if (derived.annualisedPremium < minByFreq[form.premiumFrequency]) {
      errors.push('Premium is below minimum for selected frequency');
    }
  }

  if (shouldShowFundOption(form.investmentStrategy) && !form.fundOption) {
    errors.push('Fund Option is required for Self-Managed Investment Strategy');
  }

  if (!shouldShowFundOption(form.investmentStrategy) && form.fundOption) {
    errors.push('Fund Option must be empty unless Self-Managed Investment Strategy is selected');
  }

  return errors;
}

export function shouldShowProposerFields(isProposerDifferent: boolean): boolean {
  return isProposerDifferent;
}
