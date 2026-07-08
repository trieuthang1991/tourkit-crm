import { z } from 'zod';
import { httpClient } from '../../../shared/api/httpClient';

export const loginRequestSchema = z.object({
  tenantSlug: z.string().min(1),
  email: z.string().min(1),
  password: z.string().min(1),
});

export type LoginRequest = z.infer<typeof loginRequestSchema>;

const authResponseSchema = z.object({
  accessToken: z.string(),
  refreshToken: z.string(),
  accessTokenExpiresAt: z.string(),
});

export type AuthResponse = z.infer<typeof authResponseSchema>;

export const authApi = {
  login: async (payload: LoginRequest): Promise<AuthResponse> => {
    const { data } = await httpClient.post<unknown>('/api/v1/auth/login', payload);
    return authResponseSchema.parse(data);
  },
};
