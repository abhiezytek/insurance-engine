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

  // Role is now securely embedded in the JWT token claims.
  // The X-Role header is no longer sent — all role-based authorization
  // is handled server-side via JWT claim extraction.

  config.headers = headers;
  return config;
});
