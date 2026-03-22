import { apiClient } from '../utils/apiClient';
import type { Product, ProductVersion } from '../api';

export type YpygProductCategory = 'Traditional' | 'ULIP';

export interface YpygProductMeta {
  code: string;
  displayName: string;
  category: YpygProductCategory;
  options: string[];
  channels: string[];
  uinVersions: string[];
  pptOptions: number[];
  ptOptionsByPpt: Record<number, number[]>;
  frequencyOptions: string[];
  fundOptions?: string[];
  versions?: string[];
  modalFactors?: Record<string, number>;
  sumAssuredFormula?: { basis: 'annualPremium' | 'modalPremium'; multiple: number };
}

export type YpygProductMap = Record<YpygProductCategory, YpygProductMeta>;

const COMMON_CHANNELS = ['Corporate Agency', 'Direct Marketing', 'Online', 'Broker', 'Agency', 'Web Aggregator', 'Insurance Marketing Firm'];
const COMMON_FREQUENCIES = ['Yearly', 'Half-Yearly', 'Quarterly', 'Monthly'];

export const DEFAULT_YPYG_PRODUCTS: YpygProductMap = {
  Traditional: {
    code: 'CENTURY_INCOME',
    displayName: 'Century Income (Traditional)',
    category: 'Traditional',
    options: ['Immediate', 'Deferred', 'Twin'],
    channels: COMMON_CHANNELS,
    uinVersions: ['142N066V02'],
    pptOptions: [7, 10, 12],
    ptOptionsByPpt: { 7: [15, 20], 10: [20, 25], 12: [25] },
    frequencyOptions: COMMON_FREQUENCIES,
    modalFactors: {
      Yearly: 1,
      'Half-Yearly': 2,
      Quarterly: 4,
      Monthly: 12,
    },
    sumAssuredFormula: { basis: 'annualPremium', multiple: 10 },
  },
  ULIP: {
    code: 'EWEALTH-ROYALE',
    displayName: 'e-Wealth Royale (ULIP)',
    category: 'ULIP',
    options: ['Platinum', 'Platinum Plus'],
    channels: COMMON_CHANNELS,
    uinVersions: ['142L079V03'],
    pptOptions: [5, 7, 10],
    ptOptionsByPpt: { 5: [10, 15], 7: [15], 10: [15, 20] },
    frequencyOptions: COMMON_FREQUENCIES,
    fundOptions: ['Bluechip Fund', 'Balanced Fund', 'Bond Fund'],
    modalFactors: {
      Yearly: 1,
      'Half-Yearly': 2,
      Quarterly: 4,
      Monthly: 12,
    },
  },
};

// Best-effort loader: merges catalogue data with default configs so UI always has rules.
export async function loadYpygProductMap(): Promise<YpygProductMap> {
  try {
    const res = await apiClient.get<Product[]>('/api/admin/products');
    const map: YpygProductMap = { ...DEFAULT_YPYG_PRODUCTS };
    res.data.forEach((p: Product) => {
      const isUlip = (p.productType ?? p.name ?? '').toLowerCase().includes('ulip');
      const key: YpygProductCategory = isUlip ? 'ULIP' : 'Traditional';
      const defaults = DEFAULT_YPYG_PRODUCTS[key];
      map[key] = {
        ...defaults,
        code: p.code || defaults.code,
        displayName: p.name || defaults.displayName,
        category: key,
        options: defaults.options,
        versions: p.versions?.map((v: ProductVersion) => v.version) ?? defaults.versions,
      };
    });
    return map;
  } catch {
    return DEFAULT_YPYG_PRODUCTS;
  }
}
