import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
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
  annualPremium: number;
  ppt: number;
  policyTerm: number;
  entryAge: number;
  option: 'Immediate' | 'Deferred' | 'Twin';
  channel: 'Online' | 'StaffDirect' | 'Other';
  premiumsPaid?: number;
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
  annualPremium: number;
  ppt: number;
  policyTerm: number;
  entryAge: number;
  option: string;
  channel: string;
  sumAssuredOnDeath: number;
  guaranteedMaturityBenefit: number;
  maxLoanAmount: number;
  yearlyTable: BenefitIllustrationRow[];
}

export const getBatches = () => api.get<UploadBatch[]>('/api/upload/batches');
export const runBenefitIllustration = (req: BenefitIllustrationRequest) =>
  api.post<BenefitIllustrationResult>('/api/benefit-illustration/calculate', req);

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
  productCode: string;
  gender: 'Male' | 'Female';
  dateOfBirth: string;        // ISO date string
  entryAge: number;
  policyTerm: number;
  ppt: number;
  annualizedPremium: number;
  sumAssured: number;
  premiumFrequency: 'Yearly' | 'HalfYearly' | 'Quarterly' | 'Monthly';
  fundAllocations: UlipFundAllocation[];
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

export interface UlipCalculationResult {
  policyNumber: string;
  customerName: string;
  productCode: string;
  productName: string;
  gender: string;
  entryAge: number;
  policyTerm: number;
  ppt: number;
  annualizedPremium: number;
  sumAssured: number;
  premiumFrequency: string;
  maturityBenefit4: number;
  maturityBenefit8: number;
  irdaiDisclaimer: string;
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

