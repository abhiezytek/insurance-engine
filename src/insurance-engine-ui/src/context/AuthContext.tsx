import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import axios from 'axios';
import { API_BASE_URL } from '../utils/apiClient';

export interface AuthUser {
  username: string;
  role: string;
  token: string;
  expiresAt: string;
}

interface AuthContextValue {
  user: AuthUser | null;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
  /** When true the user must change their password before accessing the app. */
  forcePasswordChange: boolean;
  /** Temp token used for the change-password API call during forced change. */
  tempToken: string | null;
  /** Submit forced password change, returns the full auth user on success. */
  changePassword: (newPassword: string, currentPassword?: string) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const STORAGE_KEY = 'precision_pro_auth';
const TEMP_TOKEN_KEY = 'precision_pro_temp_token';
// Note: localStorage is used here for SPA convenience. In a production environment with
// stricter XSS requirements, consider migrating to httpOnly cookies via a BFF (Backend For Frontend).

function loadStoredAuth(): AuthUser | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as AuthUser;
    // Check expiry
    if (new Date(parsed.expiresAt) < new Date()) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(loadStoredAuth);
  const [forcePasswordChange, setForcePasswordChange] = useState(false);
  const [tempToken, setTempToken] = useState<string | null>(
    () => sessionStorage.getItem(TEMP_TOKEN_KEY)
  );

  const login = useCallback(async (username: string, password: string) => {
    const res = await axios.post(`${API_BASE_URL}/api/auth/login`, { username, password });
    const data = res.data as {
      token?: string;
      username?: string;
      role?: string;
      expiresAt?: string;
      requiresPasswordChange?: boolean;
      tempToken?: string;
      message?: string;
    };

    if (data.requiresPasswordChange && data.tempToken) {
      // Store temp token and enter forced password change mode
      sessionStorage.setItem(TEMP_TOKEN_KEY, data.tempToken);
      setTempToken(data.tempToken);
      setForcePasswordChange(true);
      return;
    }

    // Normal login
    const authUser: AuthUser = {
      token: data.token!,
      username: data.username!,
      role: data.role!,
      expiresAt: data.expiresAt!,
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(authUser));
    setUser(authUser);
  }, []);

  const changePassword = useCallback(async (newPassword: string, currentPassword?: string) => {
    const token = tempToken ?? user?.token;
    if (!token) throw new Error('No authentication token available.');

    const res = await axios.post(
      `${API_BASE_URL}/api/auth/change-password`,
      { newPassword, currentPassword },
      { headers: { Authorization: `Bearer ${token}` } }
    );

    const data = res.data as { token: string; username: string; role: string; expiresAt: string };

    // Clear temp token state
    sessionStorage.removeItem(TEMP_TOKEN_KEY);
    setTempToken(null);
    setForcePasswordChange(false);

    // Set full auth
    const authUser: AuthUser = {
      token: data.token,
      username: data.username,
      role: data.role,
      expiresAt: data.expiresAt,
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(authUser));
    setUser(authUser);
  }, [tempToken, user?.token]);

  const logout = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY);
    sessionStorage.removeItem(TEMP_TOKEN_KEY);
    setTempToken(null);
    setForcePasswordChange(false);
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{
      user,
      login,
      logout,
      isAuthenticated: !!user,
      forcePasswordChange,
      tempToken,
      changePassword,
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
}
