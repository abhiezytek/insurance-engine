import { apiClient } from '../utils/apiClient';
import type { Product, ProductVersion } from '../api';

export interface YpygProductMeta {
  code: string;
  displayName: string;
  options: string[];
  channels?: string[];
  versions?: string[];
}

export type YpygProductMap = Record<string, YpygProductMeta>;

export const DEFAULT_YPYG_PRODUCTS: YpygProductMap = {
  Traditional: {
    code: 'CENTURY_INCOME',
    displayName: 'Endowment (Traditional)',
    options: ['Immediate', 'Deferred', 'Twin'],
    channels: ['Online', 'StaffDirect', 'Other'],
    versions: [],
  },
  ULIP: {
    code: 'EWEALTH-ROYALE',
    displayName: 'ULIP (Unit Linked)',
    options: ['Platinum', 'Platinum Plus'],
    channels: ['Online', 'StaffDirect', 'Other'],
    versions: [],
  },
};

// Best-effort loader: falls back to defaults if the admin catalogue endpoint isn't ready.
export async function loadYpygProductMap(): Promise<YpygProductMap> {
  try {
    const res = await apiClient.get<Product[]>('/api/admin/products');
    const map: YpygProductMap = { ...DEFAULT_YPYG_PRODUCTS };
    res.data.forEach((p: Product) => {
      const key = p.productType || p.name || p.code;
      const defaultOptions =
        (p.productType ?? '').toLowerCase().includes('ulip')
          ? DEFAULT_YPYG_PRODUCTS.ULIP.options
          : DEFAULT_YPYG_PRODUCTS.Traditional.options;
      map[key] = {
        code: p.code,
        displayName: p.name,
        options: defaultOptions,
        channels: DEFAULT_YPYG_PRODUCTS.Traditional.channels,
        versions: p.versions?.map((v: ProductVersion) => v.version) ?? [],
      };
    });
    return map;
  } catch {
    return DEFAULT_YPYG_PRODUCTS;
  }
}
