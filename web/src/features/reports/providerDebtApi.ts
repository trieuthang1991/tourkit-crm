import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const providerDebtRowSchema = z.object({
  providerId: z.string().uuid(),
  providerName: z.string(),
  totalCost: z.number(),
  paid: z.number(),
  outstanding: z.number(),
  // Cột tuổi nợ (aging) — nullish-safe: hàng cũ thiếu field vẫn parse được.
  current: z.number().nullish(),
  d30: z.number().nullish(),
  d60: z.number().nullish(),
  d90Plus: z.number().nullish(),
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

/* ---------------- Drill-down: lịch sử giao dịch theo NCC ---------------- */

export const providerTxnSchema = z.object({
  date: z.string(),
  type: z.enum(['cost', 'payment']),
  refCode: z.string().nullish(),
  description: z.string().nullish(),
  debit: z.number().nullish(),
  credit: z.number().nullish(),
  runningRemaining: z.number().nullish(),
});
export type ProviderTxn = z.infer<typeof providerTxnSchema>;

export const providerTxnResponseSchema = z.object({
  providerId: z.string(),
  providerName: z.string(),
  summary: z.object({
    totalCost: z.number().nullish(),
    totalPaid: z.number().nullish(),
    remaining: z.number().nullish(),
  }),
  transactions: z.array(providerTxnSchema),
});
export type ProviderTxnResponse = z.infer<typeof providerTxnResponseSchema>;

export function useProviderTransactions(providerId: string | null) {
  return useQuery({
    queryKey: ['reports', 'provider-debt', providerId, 'transactions'] as const,
    enabled: !!providerId,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(
        `/api/v1/reports/provider-debt/${providerId}/transactions`,
      );
      return providerTxnResponseSchema.parse(data);
    },
  });
}
