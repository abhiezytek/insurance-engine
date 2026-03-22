import { describe, expect, it } from 'vitest';
import { DEFAULT_YPYG_PRODUCTS } from '../config/products';
import {
  applyPolicyPrefill,
  buildDefaultForm,
  buildVisibleFields,
  computeSumAssured,
  deriveAnnualPremium,
  resolvePtOptions,
  type BuildContext,
} from './fieldSchema';
import { MOCK_TRADITIONAL_POLICY, MOCK_ULIP_POLICY } from './mockPolicies';

describe('ypyg field schema helpers', () => {
  it('resolves PT options based on PPT rules', () => {
    const pts = resolvePtOptions(DEFAULT_YPYG_PRODUCTS.Traditional, 7);
    expect(pts).toEqual([15, 20]);
  });

  it('hides UIN dropdown when only one version is available', () => {
    const form = buildDefaultForm(DEFAULT_YPYG_PRODUCTS);
    const ctx: BuildContext = {
      form,
      product: DEFAULT_YPYG_PRODUCTS.Traditional,
      ptOptions: resolvePtOptions(DEFAULT_YPYG_PRODUCTS.Traditional, form.ppt),
    };
    const keys = buildVisibleFields(ctx).map(f => f.key);
    expect(keys).not.toContain('uin');
  });

  it('computes sum assured when a formula exists', () => {
    const product = DEFAULT_YPYG_PRODUCTS.Traditional;
    const annual = deriveAnnualPremium(60000, 'Yearly', product);
    const form = { ...buildDefaultForm(DEFAULT_YPYG_PRODUCTS), modalPremium: 60000, annualPremium: annual };
    expect(computeSumAssured(form, product)).toBe(annual * 10);
  });

  it('prefills policy lookup into form state for ULIP', () => {
    const form = buildDefaultForm(DEFAULT_YPYG_PRODUCTS);
    const filled = applyPolicyPrefill(MOCK_ULIP_POLICY, form, DEFAULT_YPYG_PRODUCTS);
    expect(filled.productKey).toBe('ULIP');
    expect(filled.policyYear).toBe(MOCK_ULIP_POLICY.premiumsPaid);
    expect(filled.annualPremium).toBeGreaterThan(0);
    expect(filled.policyStatus).toBe('In-Force');
  });

  it('keeps traditional values when applying mock traditional policy', () => {
    const form = buildDefaultForm(DEFAULT_YPYG_PRODUCTS);
    const filled = applyPolicyPrefill(MOCK_TRADITIONAL_POLICY, form, DEFAULT_YPYG_PRODUCTS);
    expect(filled.productKey).toBe('Traditional');
    expect(filled.sumAssured).toBeGreaterThan(0);
    expect(filled.ppt).toBe(MOCK_TRADITIONAL_POLICY.premiumPayingTerm);
  });
});
