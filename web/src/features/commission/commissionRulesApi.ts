import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { commissionRuleSchema } from './commissionRuleTypes';

const KEY = ['commission-rules'];

export type CommissionRuleFilter = { q?: string; userId?: string; status?: number };

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useCommissionRules(page: number, size: number, filter: CommissionRuleFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/commission-rules', { params: cleanParams({ page, size, ...filter }) });
      return pagedSchema(commissionRuleSchema).parse(data);
    },
  });
}

export const commissionRuleStatsSchema = z.object({
  total: z.number(),
  active: z.number(),
  inactive: z.number(),
  avgPercentage: z.number(),
});

export function useCommissionRuleStats() {
  return useQuery({
    queryKey: [...KEY, 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/commission-rules/stats');
      return commissionRuleStatsSchema.parse(data);
    },
  });
}

const userOptionSchema = z.object({ id: z.string().uuid(), fullName: z.string(), email: z.string() });

export function useUserOptions() {
  return useQuery({
    queryKey: ['users', 'options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userOptionSchema).parse(data);
    },
  });
}
