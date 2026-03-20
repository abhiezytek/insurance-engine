import axios, { type AxiosRequestHeaders } from 'axios';

export const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://ezytek1706-003-site3.rtempurl.com';

// Shared axios instance with a single place to attach auth headers and base URL
export const apiClient = axios.create({ baseURL: API_BASE_URL });

apiClient.interceptors.request.use(config => {
  // Prefer the new auth storage key; fall back to the legacy one for compatibility
  const storedAuth = localStorage.getItem('precision_pro_auth');
  let token: string | undefined;
  if (storedAuth) {
    try {
      token = (JSON.parse(storedAuth) as { token?: string }).token;
    } catch {
      token = undefined;
    }
  }
  token = token ?? localStorage.getItem('auth_token') ?? undefined;
  if (token) {
    const headers: AxiosRequestHeaders = (config.headers ?? {}) as AxiosRequestHeaders;
    headers.Authorization = `Bearer ${token}`;
    config.headers = headers;
  }
  return config;
});
