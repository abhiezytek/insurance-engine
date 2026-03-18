import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://ezytek1706-003-site3.rtempurl.com',
});

export interface Product {
  id: number;
  name: string;
  code: string;
  productType: string;
  insurer: { name: string; code: string };
  versions: ProductVersion[];
}

export interface ProductVersion {
  id: number;
  version: string;
  isActive: boolean;
  effectiveDate: string;
}

export interface ProductParameter {
  id: number;
  name: string;
  dataType: string;
  isRequired: boolean;
  description?: string;
}

export interface ProductFormula {
  id: number;
  name: string;
  expression: string;
  executionOrder: number;
  description?: string;
}

export interface CalculationResult {
  productCode: string;
  version: string;
  results: Record<string, number>;
}

export const getProducts = () => api.get<Product[]>('/api/admin/products');
export const getParameters = (productVersionId: number) =>
  api.get<ProductParameter[]>(`/api/admin/parameters?productVersionId=${productVersionId}`);
export const getFormulas = (productVersionId: number) =>
  api.get<ProductFormula[]>(`/api/admin/formulas?productVersionId=${productVersionId}`);
export const createFormula = (data: Omit<ProductFormula, 'id'> & { productVersionId: number }) =>
  api.post<ProductFormula>('/api/admin/formulas', data);
export const updateFormula = (id: number, data: Omit<ProductFormula, 'id'>) =>
  api.put<ProductFormula>(`/api/admin/formulas/${id}`, data);
export const deleteFormula = (id: number) => api.delete(`/api/admin/formulas/${id}`);
export const testFormula = (id: number, expression: string, parameters: Record<string, number>) =>
  api.post(`/api/admin/formulas/${id}/test`, { expression, parameters });
export const runCalculation = (productCode: string, version: string | null, parameters: Record<string, number>) =>
  api.post<CalculationResult>('/api/calculation/traditional', { productCode, version, parameters });
export const uploadFile = (file: File, uploadType: string, productVersionId: number) => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post(`/api/upload?uploadType=${uploadType}&productVersionId=${productVersionId}`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
};

export interface UploadBatch {
  id: number;
  uploadType: string;
  fileName: string;
  totalRows: number;
  processedRows: number;
  errorRows: number;
  uploadedAt: string;
}

export interface BenefitIllustrationRequest {
  annualisedPremium?: number;
  annualPremium: number;
  ppt: number;
  policyTerm: number;
  entryAge: number;
  nameOfLifeAssured?: string;
  nameOfPolicyHolder?: string;
  ageOfPolicyHolder?: number;
  option: 'Immediate' | 'Deferred' | 'Twin';
  channel: string;
  gender?: 'Male' | 'Female';
  premiumFrequency?: 'Yearly' | 'Half Yearly' | 'Quarterly' | 'Monthly';
  standardAgeProof?: boolean;
  staffPolicy?: boolean;
  premiumsPaid?: number;
  sumAssured?: number;
  isPreIssuance?: boolean;
  riskCommencementDate?: string | null;
}

export interface BenefitIllustrationRow {
  policyYear: number;
  annualPremium: number;
  totalPremiumsPaid: number;
  guaranteedIncome: number;
  loyaltyIncome: number;
  totalIncome: number;
  cumulativeSurvivalBenefits: number;
  gsv: number;
  ssv: number;
  surrenderValue: number;
  deathBenefit: number;
  maturityBenefit: number;
  isPaidUp: boolean;
}

export interface BenefitIllustrationResult {
  annualisedPremium: number;
  annualPremium: number;
  ppt: number;
  policyTerm: number;
  entryAge: number;
  option: string;
  channel: string;
  premiumFrequency: string;
  sumAssuredOnDeath: number;
  sumAssuredOnMaturity: number;
  guaranteedMaturityBenefit: number;
  maxLoanAmount: number;
  yearlyTable: BenefitIllustrationRow[];
}

export const getBatches = () => api.get<UploadBatch[]>('/api/upload/batches');
export const runBenefitIllustration = (req: BenefitIllustrationRequest) =>
  api.post<BenefitIllustrationResult>('/api/benefit-illustration/calculate', req);

export interface EndowmentProductConfig {
  pptOptions: number[];
  ptOptionsByPpt: Record<string, number[]>;
  channels: string[];
  paymentModes: string[];
}

export const getEndowmentConfig = () =>
  api.get<EndowmentProductConfig>('/api/benefit-illustration/config');

// ---------------------------------------------------------------------------
// ULIP — Unit Linked Insurance Plan
// ---------------------------------------------------------------------------

export interface UlipFundAllocation {
  fundType: string;
  allocationPercent: number;
}

export interface UlipCalculationRequest {
  policyNumber: string;
  customerName: string;
  policyholderName?: string;
  productCode: string;
  option: 'Platinum' | 'Platinum Plus';
  gender: 'Male' | 'Female';
  dateOfBirth: string;        // ISO date string
  entryAge: number;
  policyholderDateOfBirth?: string;
  policyholderAge?: number;
  policyholderGender?: 'Male' | 'Female';
  typeOfPpt?: 'Limited' | 'Till_Maturity';
  policyTerm: number;
  ppt: number;
  annualizedPremium: number;
  sumAssured: number;
  premiumFrequency: 'Yearly' | 'Half Yearly' | 'Quarterly' | 'Monthly';
  policyEffectiveDate?: string;  // ISO date string (used for birthday-month tracking)
  fundOption?: string;          // Fund option category
  investmentStrategy?: string;
  riskPreference?: 'Conservative' | 'Moderate' | 'Aggressive';
  fundAllocations: UlipFundAllocation[];
  distributionChannel?: string;
  isStaffFamily?: boolean;
  ageRiskCommencement?: number;
  standardAgeProofLA?: boolean;
  standardAgeProofPH?: boolean;
  emrClassLifeAssured?: string;   // Standard or EMR class level
  emrClassPolicyholder?: string;
  flatExtraLifeAssured?: number;  // per 1000 SAR
  flatExtraPolicyholder?: number;
  keralaFloodCess?: boolean;
}

export interface UlipIllustrationRow {
  year: number;
  age: number;
  annualPremium: number;
  premiumInvested: number;
  mortalityCharge: number;
  policyCharge: number;
  fundValue4: number;
  deathBenefit4: number;
  fundValue8: number;
  deathBenefit8: number;
}

export interface PartARow {
  year: number;
  annualizedPremium: number;
  // 4% scenario
  mortalityCharges4: number;
  arbCharges4: number;
  otherCharges4: number;
  gst4: number;
  fundAtEndOfYear4: number;
  surrenderValue4: number;
  deathBenefit4: number;
  // 8% scenario
  mortalityCharges8: number;
  arbCharges8: number;
  otherCharges8: number;
  gst8: number;
  fundAtEndOfYear8: number;
  surrenderValue8: number;
  deathBenefit8: number;
}

export interface PartBRow {
  year: number;
  annualizedPremium: number;
  premiumAllocationCharge: number;
  annualizedPremiumAfterPac: number;
  mortalityCharges: number;
  arbCharges: number;
  gst: number;
  policyAdministrationCharges: number;
  extraPremiumAllocation: number;
  fundBeforeFmc: number;
  fundManagementCharge: number;
  loyaltyAddition: number;
  wealthBooster: number;
  returnOfCharges: number;
  fundAtEndOfYear: number;
  surrenderValue: number;
  deathBenefit: number;
}

export interface UlipCalculationResult {
  policyNumber: string;
  customerName: string;
  productCode: string;
  productName: string;
  option: string;
  gender: string;
  entryAge: number;
  policyTerm: number;
  ppt: number;
  annualizedPremium: number;
  sumAssured: number;
  premiumFrequency: string;
  maturityAge: number;
  premiumInstallment: number;
  netYield4: number;
  netYield8: number;
  maturityBenefit4: number;
  maturityBenefit8: number;
  irdaiDisclaimer: string;
  // New Part A / Part B tables
  partARows: PartARow[];
  partBRows4: PartBRow[];
  partBRows8: PartBRow[];
  // Legacy table (for backward compat)
  yearlyTable: UlipIllustrationRow[];
}

export interface UlipProduct {
  id: number;
  code: string;
  name: string;
  productType: string;
}

export const getUlipProducts = () =>
  api.get<UlipProduct[]>('/api/ulip/products');

export const runUlipCalculation = (req: UlipCalculationRequest) =>
  api.post<UlipCalculationResult>('/api/ulip/calculate', req);

export const getUlipIllustration = (policyNumber: string) =>
  api.get<UlipCalculationResult>(`/api/ulip/illustration/${encodeURIComponent(policyNumber)}`);

export const uploadUlipMortality = (file: File, gender: string = 'Male') => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post(`/api/ulip/upload-mortality?gender=${gender}`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
};

export const uploadUlipCharges = (file: File, productCode: string = 'EWEALTH-ROYALE') => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post(`/api/ulip/upload-charges?productCode=${productCode}`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
};

