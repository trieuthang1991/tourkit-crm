import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { customerCommissionRuleSchema } from './types';

const KEY = ['customerCommissionRules'];

export type CustomerCommissionFilter = { customerType?: number; status?: number };

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useCustomerCommissionRules(page: number, size: number, filter: CustomerCommissionFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customer-commission-rules', { params: cleanParams({ page, size, ...filter }) });
      return pagedSchema(customerCommissionRuleSchema).parse(data);
    },
  });
}

export const customerCommissionStatsSchema = z.object({
  total: z.number(),
  active: z.number(),
  inactive: z.number(),
  avgPercentage: z.number(),
});

export function useCustomerCommissionStats() {
  return useQuery({
    queryKey: [...KEY, 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customer-commission-rules/stats');
      return customerCommissionStatsSchema.parse(data);
    },
  });
}

const customerTypeOptionSchema = z.object({ id: z.string().uuid(), code: z.number(), name: z.string() });

export function useCustomerTypeOptions() {
  return useQuery({
    queryKey: ['customer-types', 'options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customer-types');
      return z.array(customerTypeOptionSchema).parse(data);
    },
  });
}
