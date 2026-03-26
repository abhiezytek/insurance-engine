import { describe, expect, it } from 'vitest';
import {
  MODAL_FACTORS,
  deriveAnnualisedPremium,
  deriveCenturyIncomeValues,
  deriveUlipValues,
  getCenturyIncomePtOptions,
  getUlipPtOptions,
  shouldShowFundOption,
  shouldShowRiskPreference,
  validateCenturyIncome,
  validateUlip,
} from './biRules';

describe('biRules utilities', () => {
  it('derives annualised premium using modal factors', () => {
    const annualised = deriveAnnualisedPremium(10000, 'Monthly');
    expect(annualised).toBe(Math.round(10000 / MODAL_FACTORS.Monthly));
  });

  it('returns PT options for Century Income PPT 10', () => {
    expect(getCenturyIncomePtOptions(10 as any)).toEqual([20, 25]);
  });

  it('derives Century Income annualised premium and sum assured', () => {
    const result = deriveCenturyIncomeValues({
      product: 'CENTURY_INCOME',
      option: 'Immediate',
      isProposerDifferent: false,
      lifeAssuredName: '',
      lifeAssuredDob: '1990-01-01',
      lifeAssuredGender: 'Male',
      premium: 50000,
      premiumFrequency: 'Half-Yearly',
      ppt: 7,
      pt: 15,
      standardAgeProof: true,
      salesChannel: 'Agency',
      staffPolicy: false,
    });

    expect(result.annualisedPremium).toBe(Math.round(50000 / MODAL_FACTORS['Half-Yearly']));
    expect(result.sumAssured).toBe((result.annualisedPremium ?? 0) * 10);
  });

  it('validates minimum annualised premium for Century Income', () => {
    const errors = validateCenturyIncome({
      product: 'CENTURY_INCOME',
      option: 'Immediate',
      isProposerDifferent: false,
      lifeAssuredName: '',
      lifeAssuredDob: '1990-01-01',
      lifeAssuredGender: 'Male',
      premium: 300,
      premiumFrequency: 'Monthly',
      ppt: 7,
      pt: 15,
      standardAgeProof: true,
      salesChannel: 'Agency',
      staffPolicy: false,
    } as any);
    expect(errors.some(e => e.includes('Minimum annualised premium'))).toBe(true);
  });

  it('derives ULIP sum assured for single pay and regular pay', () => {
    const single = deriveUlipValues({
      product: 'EWEALTH_ROYALE',
      option: 'Platinum',
      isProposerDifferent: false,
      lifeAssuredName: '',
      lifeAssuredDob: '1990-01-01',
      lifeAssuredGender: 'Male',
      premium: 250000,
      premiumFrequency: 'Yearly',
      pptType: 'Single',
      pptYears: 1,
      pt: 10,
      investmentStrategy: 'Self-Managed Investment Strategy',
      policyEffectiveDate: '2024-01-01',
      standardAgeProof: true,
      salesChannel: 'Agency',
      staffPolicy: false,
    });
    expect(single.sumAssured).toBe(250000 * 1.25);

    const regular = deriveUlipValues({
      product: 'EWEALTH_ROYALE',
      option: 'Platinum',
      isProposerDifferent: false,
      lifeAssuredName: '',
      lifeAssuredDob: '1990-01-01',
      lifeAssuredGender: 'Male',
      premium: 60000,
      premiumFrequency: 'Yearly',
      pptType: 'Regular',
      pptYears: 20,
      pt: 20,
      investmentStrategy: 'Self-Managed Investment Strategy',
      policyEffectiveDate: '2024-01-01',
      standardAgeProof: true,
      salesChannel: 'Agency',
      staffPolicy: false,
    });
    expect(regular.sumAssured).toBe(Math.round(60000 / MODAL_FACTORS.Yearly) * 10);
  });

  it('validates ULIP PT options and fund visibility', () => {
    expect(getUlipPtOptions('Limited', 10)[0]).toBe(15);
    expect(shouldShowFundOption('Self-Managed Investment Strategy')).toBe(true);
    expect(shouldShowFundOption('Age-based Investment Strategy')).toBe(false);
  });

  it('validates ULIP minimum premium by frequency', () => {
    const errors = validateUlip({
      product: 'EWEALTH_ROYALE',
      option: 'Platinum',
      isProposerDifferent: false,
      lifeAssuredName: '',
      lifeAssuredDob: '1990-01-01',
      lifeAssuredGender: 'Male',
      premium: 300,
      premiumFrequency: 'Monthly',
      pptType: 'Limited',
      pptYears: 10,
      pt: 15,
      investmentStrategy: 'Self-Managed Investment Strategy',
      fundOption: null,
      policyEffectiveDate: '2024-01-01',
      standardAgeProof: true,
      salesChannel: 'Agency',
      staffPolicy: false,
    } as any);
    expect(errors.some(e => e.includes('below minimum'))).toBe(true);
  });

  it('rejects unsupported investment strategy values', () => {
    const errors = validateUlip({
      product: 'EWEALTH_ROYALE',
      option: 'Platinum',
      isProposerDifferent: false,
      lifeAssuredName: '',
      lifeAssuredDob: '1990-01-01',
      lifeAssuredGender: 'Male',
      premium: 60000,
      premiumFrequency: 'Yearly',
      pptType: 'Limited',
      pptYears: 10,
      pt: 15,
      investmentStrategy: 'System Managed' as any,
      policyEffectiveDate: '2024-01-01',
      standardAgeProof: true,
      salesChannel: 'Agency',
      staffPolicy: false,
    });
    expect(errors.some(e => e.includes('Unsupported Investment Strategy'))).toBe(true);
  });

  it('requires risk preference for Age-based Investment Strategy', () => {
    expect(shouldShowRiskPreference('Age-based Investment Strategy')).toBe(true);
    expect(shouldShowRiskPreference('Self-Managed Investment Strategy')).toBe(false);
    expect(shouldShowRiskPreference(null)).toBe(false);

    const errors = validateUlip({
      product: 'EWEALTH_ROYALE',
      option: 'Platinum',
      isProposerDifferent: false,
      lifeAssuredName: '',
      lifeAssuredDob: '1990-01-01',
      lifeAssuredGender: 'Male',
      premium: 60000,
      premiumFrequency: 'Yearly',
      pptType: 'Limited',
      pptYears: 10,
      pt: 15,
      investmentStrategy: 'Age-based Investment Strategy',
      riskPreference: null,
      policyEffectiveDate: '2024-01-01',
      standardAgeProof: true,
      salesChannel: 'Agency',
      staffPolicy: false,
    });
    expect(errors.some(e => e.includes('Risk Preference is required'))).toBe(true);
  });
});
