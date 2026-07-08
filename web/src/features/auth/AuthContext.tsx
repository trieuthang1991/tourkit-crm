import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { authApi } from './api/authApi';
import type { LoginRequest } from './api/authApi';
import { clearTokens, getAccessToken, setTokens } from '../../shared/api/httpClient';

type AuthContextValue = {
  token: string | null;
  login: (payload: LoginRequest) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

type AuthProviderProps = {
  children: ReactNode;
};

export function AuthProvider({ children }: AuthProviderProps) {
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

  const value = useMemo<AuthContextValue>(() => ({ token, login, logout }), [token, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components -- context + hook dùng chung 1 file là chuẩn cho Context Provider.
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth phải được dùng bên trong AuthProvider');
  }
  return ctx;
}
