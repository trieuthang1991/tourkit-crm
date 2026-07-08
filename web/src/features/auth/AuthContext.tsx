import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { authApi } from './api/authApi';
import type { LoginRequest } from './api/authApi';
import { clearTokens, getAccessToken, setTokens } from '../../shared/api/httpClient';
import { decodeToken } from '../../shared/auth/jwt';

type AuthContextValue = {
  token: string | null;
  email: string | null;
  permissions: string[];
  has: (perm: string) => boolean;
  login: (payload: LoginRequest) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => getAccessToken());

  const login = useCallback(async (payload: LoginRequest) => {
    const result = await authApi.login(payload);
    setTokens(result.accessToken, result.refreshToken);
    setToken(result.accessToken);
  }, []);

  const logout = useCallback(() => {
    clearTokens();
    setToken(null);
  }, []);

  const claims = useMemo(() => decodeToken(token), [token]);
  const permissions = useMemo(() => claims?.permissions ?? [], [claims]);

  const has = useCallback((perm: string) => permissions.includes(perm), [permissions]);

  const value = useMemo<AuthContextValue>(
    () => ({ token, email: claims?.email ?? null, permissions, has, login, logout }),
    [token, claims, permissions, has, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components -- context + hook cùng file là chuẩn.
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth phải được dùng bên trong AuthProvider');
  }
  return ctx;
}
