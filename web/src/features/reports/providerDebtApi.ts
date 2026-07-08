import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const providerDebtRowSchema = z.object({
  providerId: z.string().uuid(),
  providerName: z.string(),
  totalCost: z.number(),
  paid: z.number(),
  outstanding: z.number(),
});
export type ProviderDebtRow = z.infer<typeof providerDebtRowSchema>;

const providerDebtKey = ['reports', 'provider-debt'] as const;

export function useProviderDebt() {
  return useQuery({
    queryKey: providerDebtKey,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/reports/provider-debt');
      return z.array(providerDebtRowSchema).parse(data);
    },
  });
}
