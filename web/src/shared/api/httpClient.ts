import axios from 'axios';

const ACCESS_TOKEN_KEY = 'tourkit.accessToken';
const REFRESH_TOKEN_KEY = 'tourkit.refreshToken';

export function getAccessToken(): string | null {
  return localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_KEY);
}

export function setTokens(accessToken: string, refreshToken: string): void {
  localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
}

export function clearTokens(): void {
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
}

const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5075';

export const httpClient = axios.create({
  baseURL: API_BASE,
});

httpClient.interceptors.request.use((config) => {
  const token = getAccessToken();
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
  }
  return config;
});

function redirectToLogin(): void {
  clearTokens();
  if (window.location.pathname !== '/login') {
    window.location.href = '/login';
  }
}

// Single-flight: nhiều request cùng lúc gặp 401 chỉ gọi /auth/refresh MỘT lần.
let refreshInFlight: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) {
    return null;
  }
  try {
    // Dùng axios "trần" (không qua interceptor) để tránh vòng lặp refresh.
    const { data } = await axios.post<{ accessToken: string; refreshToken: string }>(
      `${API_BASE}/api/v1/auth/refresh`,
      { refreshToken },
    );
    setTokens(data.accessToken, data.refreshToken);
    return data.accessToken;
  } catch {
    return null;
  }
}

httpClient.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    if (!axios.isAxiosError(error) || error.response?.status !== 401 || !error.config) {
      return Promise.reject(error);
    }

    const original = error.config as typeof error.config & { _retried?: boolean };
    const url = original.url ?? '';

    // Chính login/refresh 401 hoặc đã thử refresh 1 lần rồi → hết cứu, về /login.
    if (original._retried || url.includes('/auth/login') || url.includes('/auth/refresh')) {
      redirectToLogin();
      return Promise.reject(error);
    }

    original._retried = true;
    refreshInFlight = refreshInFlight ?? refreshAccessToken();
    const newToken = await refreshInFlight;
    refreshInFlight = null;

    if (!newToken) {
      redirectToLogin();
      return Promise.reject(error);
    }

    // Gọi lại request gốc — request interceptor sẽ tự gắn access token mới.
    return httpClient(original);
  },
);
