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
