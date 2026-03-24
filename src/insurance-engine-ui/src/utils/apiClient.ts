import axios, { type AxiosRequestHeaders } from 'axios';

export const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://ezytek1706-003-site3.rtempurl.com';

// Shared axios instance with a single place to attach auth headers and base URL
export const apiClient = axios.create({ baseURL: API_BASE_URL });

apiClient.interceptors.request.use(config => {
  // Prefer the new auth storage key; fall back to the legacy one for compatibility
  const storedAuthJson = localStorage.getItem('precision_pro_auth');
  let parsedAuth: { token?: string; role?: string } | undefined;
  if (storedAuthJson) {
    try {
      parsedAuth = JSON.parse(storedAuthJson) as { token?: string; role?: string };
    } catch {
      parsedAuth = undefined;
    }
  }
  const token = parsedAuth?.token ?? localStorage.getItem('auth_token') ?? undefined;
  const headers: AxiosRequestHeaders = (config.headers ?? {}) as AxiosRequestHeaders;
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  // Attach role header for Admin APIs (header-driven mock RBAC).
  if (parsedAuth?.role) {
    headers['X-Role'] = parsedAuth.role;
  }
  const legacyRole = localStorage.getItem('auth_role');
  if (!headers['X-Role'] && legacyRole) headers['X-Role'] = legacyRole;

  // Ensure Admin screens still work during investigation
  if (!headers['X-Role'] && (config.url?.startsWith('/api/admin') || config.url?.includes('/api/admin')
      || config.url?.startsWith('/api/configuration') || config.url?.includes('/api/configuration')
      || config.url?.startsWith('/api/usermgmt') || config.url?.includes('/api/usermgmt'))) {
    headers['X-Role'] = 'Admin';
  }

  config.headers = headers;
  return config;
});
