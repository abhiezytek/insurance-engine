import { DEFAULT_YPYG_PRODUCTS, type YpygProductCategory, type YpygProductMap, type YpygProductMeta } from '../config/products';

export const YES_NO_OPTIONS = ['Yes', 'No'] as const;
export const POLICY_STATUSES = ['In-Force', 'Paid-Up', 'Lapsed', 'Revived', 'Discontinued'] as const;

export interface YpygFormState {
  policyNumber: string;
  productKey: YpygProductCategory;
  productVersion: string;
  uin: string;
  productOption: string;
  age: number;
  gender: string;
  premiumFrequency: string;
  ppt: number;
  pt: number;
  modalPremium: number;
  annualPremium: number;
  riskCommencementDate: string;
  distributionChannel: string;
  standardAgeProof: (typeof YES_NO_OPTIONS)[number];
  sumAssured: number;
  staffPolicy: (typeof YES_NO_OPTIONS)[number];
  policyYear: number;
  policyStatus: string;
  fundOption?: string;
  customerName: string;
  dateOfBirth?: string;
  premiumStatus?: string;
  pendingPremiums?: number;
  survivalBenefitPaid?: number;
}

export interface PolicyLookupModel {
  policyNumber: string;
  customerName: string;
  productType: string;
  productCode: string;
  productCategory: string;
  uin: string;
  annualPremium: number;
  policyTerm: number;
  premiumPayingTerm: number;
  premiumsPaid: number;
  sumAssured: number;
  fundValue: number;
  policyStatus: string;
  option: string;
  channel: string;
  entryAge: number;
  gender: string;
  dateOfBirth: string;
  premiumFrequency: string;
  premiumStatus: string;
  dateOfCommencement: string;
  riskCommencementDate: string;
  pendingPremiums: number;
  survivalBenefitPaid: number;
  investmentStrategy?: string;
}

export type FieldType = 'text' | 'number' | 'select' | 'date';

export interface YpygFieldDefinition {
  key: keyof YpygFormState;
  label: string;
  type: FieldType;
  options?: string[] | ((ctx: BuildContext) => string[]);
  columns?: 1 | 2;
  helperText?: string;
  visible?: (ctx: BuildContext) => boolean;
  readOnly?: (ctx: BuildContext) => boolean;
}

export interface BuildContext {
  form: YpygFormState;
  product: YpygProductMeta;
  ptOptions: number[];
}

export const YPYG_FIELD_DEFINITIONS: YpygFieldDefinition[] = [
  { key: 'productOption', label: 'Product Option', type: 'select', options: ctx => ctx.product.options },
  {
    key: 'uin',
    label: 'UIN',
    type: 'select',
    options: ctx => ctx.product.uinVersions,
    visible: ctx => ctx.product.uinVersions.length > 1,
    readOnly: ctx => ctx.product.uinVersions.length === 1,
  },
  { key: 'age', label: 'Age', type: 'number' },
  { key: 'gender', label: 'Gender', type: 'select', options: ['Male', 'Female', 'Transgender'] },
  { key: 'premiumFrequency', label: 'Premium Frequency', type: 'select', options: ctx => ctx.product.frequencyOptions },
  { key: 'ppt', label: 'Premium Paying Term (PPT)', type: 'select' },
  { key: 'pt', label: 'Policy Term (PT)', type: 'select' },
  {
    key: 'modalPremium',
    label: 'Modal Premium (Installment)',
    type: 'number',
    helperText: 'Installment premium based on selected frequency',
  },
  {
    key: 'annualPremium',
    label: 'Annualized Premium',
    type: 'number',
    readOnly: ctx => true,
    helperText: 'Auto-calculated from modal premium and frequency',
  },
  {
    key: 'sumAssured',
    label: 'Sum Assured',
    type: 'number',
    helperText: 'Auto-calculated where product provides a formula',
    readOnly: ctx => Boolean(ctx.product.sumAssuredFormula),
  },
  { key: 'riskCommencementDate', label: 'Risk Commencement Date', type: 'date' },
  {
    key: 'distributionChannel',
    label: 'Distribution Channel',
    type: 'select',
    options: ctx => ctx.product.channels,
  },
  { key: 'standardAgeProof', label: 'Standard Age Proof', type: 'select', options: [...YES_NO_OPTIONS] },
  { key: 'staffPolicy', label: 'Staff Policy', type: 'select', options: [...YES_NO_OPTIONS] },
  {
    key: 'policyYear',
    label: 'Policy Year / Premiums Paid',
    type: 'number',
    helperText: 'Number of years premium already paid',
  },
  { key: 'policyStatus', label: 'Policy Status', type: 'select', options: [...POLICY_STATUSES] },
  {
    key: 'fundOption',
    label: 'Fund Option',
    type: 'select',
    options: ctx => ctx.product.fundOptions ?? [],
    visible: ctx => ctx.product.category === 'ULIP' && Boolean(ctx.product.fundOptions?.length),
  },
  {
    key: 'customerName',
    label: 'Customer Name',
    type: 'text',
    columns: 2,
  },
];

export function deriveAnnualPremium(modalPremium: number, frequency: string, product?: YpygProductMeta): number {
  const factor = product?.modalFactors?.[frequency] ?? 1;
  return Math.round((modalPremium || 0) * factor);
}

export function computeSumAssured(form: YpygFormState, product: YpygProductMeta): number {
  if (product.sumAssuredFormula) {
    const base = product.sumAssuredFormula.basis === 'modalPremium' ? form.modalPremium : form.annualPremium;
    return Math.round(base * product.sumAssuredFormula.multiple);
  }
  return form.sumAssured;
}

export function resolvePtOptions(product: YpygProductMeta, ppt: number): number[] {
  return product.ptOptionsByPpt[ppt] ?? [];
}

export function buildDefaultForm(productMap: YpygProductMap = DEFAULT_YPYG_PRODUCTS): YpygFormState {
  const product = productMap.Traditional;
  const firstPpt = product.pptOptions[0];
  const firstPt = resolvePtOptions(product, firstPpt)[0] ?? firstPpt;
  const modalPremium = 50000;
  const annualPremium = deriveAnnualPremium(modalPremium, product.frequencyOptions[0], product);
  const base: YpygFormState = {
    policyNumber: '',
    productKey: 'Traditional',
    productVersion: '',
    uin: product.uinVersions[0] ?? '',
    productOption: product.options[0],
    age: 35,
    gender: 'Male',
    premiumFrequency: product.frequencyOptions[0],
    ppt: firstPpt,
    pt: firstPt,
    modalPremium,
    annualPremium,
    riskCommencementDate: '',
    distributionChannel: product.channels[0],
    standardAgeProof: 'Yes',
    sumAssured: 500000,
    staffPolicy: 'No',
    policyYear: 1,
    policyStatus: 'In-Force',
    fundOption: product.fundOptions?.[0],
    customerName: '',
  };
  return { ...base, sumAssured: computeSumAssured(base, product) };
}

export function applyProductDefaults(form: YpygFormState, product: YpygProductMeta): YpygFormState {
  const ppt = product.pptOptions[0];
  const pt = resolvePtOptions(product, ppt)[0] ?? ppt;
  const modalPremium = form.modalPremium || 50000;
  const annualPremium = deriveAnnualPremium(modalPremium, product.frequencyOptions[0], product);
  return {
    ...form,
    productKey: product.category,
    uin: product.uinVersions[0] ?? form.uin,
    productOption: product.options[0],
    premiumFrequency: product.frequencyOptions[0],
    ppt,
    pt,
    modalPremium,
    annualPremium,
    distributionChannel: product.channels[0],
    fundOption: product.fundOptions?.[0],
    sumAssured: computeSumAssured(
      {
        ...form,
        modalPremium,
        annualPremium,
      },
      product,
    ),
  };
}

export function buildVisibleFields(ctx: BuildContext): YpygFieldDefinition[] {
  return YPYG_FIELD_DEFINITIONS.filter(def => (def.visible ? def.visible(ctx) : true)).map(def => {
    if (def.key === 'pt') {
      return { ...def, options: ctx.ptOptions.map(String) };
    }
    if (def.key === 'ppt') {
      return { ...def, options: ctx.product.pptOptions.map(String) };
    }
    if (def.options) {
      const opts = typeof def.options === 'function' ? def.options(ctx) : def.options;
      return { ...def, options: opts };
    }
    return def;
  });
}

export function applyPolicyPrefill(
  policy: PolicyLookupModel,
  currentForm: YpygFormState,
  productMap: YpygProductMap = DEFAULT_YPYG_PRODUCTS,
): YpygFormState {
  const productKey: YpygProductCategory =
    (policy.productCategory as YpygProductCategory) === 'ULIP' ? 'ULIP' : 'Traditional';
  const product = productMap[productKey] ?? productMap.Traditional;
  const freqFactor = product.modalFactors?.[policy.premiumFrequency] ?? 1;
  const modalPremium = freqFactor ? Math.round(policy.annualPremium / freqFactor) : policy.annualPremium;
  const annualPremium = deriveAnnualPremium(modalPremium, policy.premiumFrequency, product);
  const ppt = policy.premiumPayingTerm || product.pptOptions[0];
  const pt = policy.policyTerm || resolvePtOptions(product, ppt)[0] || product.ptOptionsByPpt[ppt]?.[0] || ppt;

  const base: YpygFormState = {
    ...currentForm,
    productKey,
    uin: policy.uin || product.uinVersions[0] || '',
    productOption: policy.option || product.options[0],
    age: policy.entryAge || currentForm.age,
    gender: policy.gender || currentForm.gender,
    premiumFrequency: policy.premiumFrequency || product.frequencyOptions[0],
    ppt,
    pt,
    modalPremium,
    annualPremium,
    riskCommencementDate: policy.riskCommencementDate
      ? policy.riskCommencementDate.toString().slice(0, 10)
      : currentForm.riskCommencementDate,
    distributionChannel: policy.channel || product.channels[0],
    standardAgeProof: currentForm.standardAgeProof,
    sumAssured: policy.sumAssured || computeSumAssured(currentForm, product),
    staffPolicy: currentForm.staffPolicy,
    policyYear: policy.premiumsPaid || currentForm.policyYear,
    policyStatus: policy.policyStatus || currentForm.policyStatus,
    fundOption: currentForm.fundOption ?? product.fundOptions?.[0],
    customerName: policy.customerName || currentForm.customerName,
    dateOfBirth: policy.dateOfBirth,
    premiumStatus: policy.premiumStatus,
    pendingPremiums: policy.pendingPremiums,
    survivalBenefitPaid: policy.survivalBenefitPaid,
  };

  return {
    ...base,
    sumAssured: product.sumAssuredFormula ? computeSumAssured(base, product) : base.sumAssured,
  };
}
