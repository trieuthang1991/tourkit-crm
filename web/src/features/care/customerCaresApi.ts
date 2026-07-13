import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { customerCareSchema } from './customerCareTypes';

const KEY = ['customerCares'];

export type CustomerCareFilter = { q?: string; customerId?: string; assignedToUserId?: string; status?: number };

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useCustomerCares(page: number, size: number, filter: CustomerCareFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customer-cares', { params: cleanParams({ page, size, ...filter }) });
      return pagedSchema(customerCareSchema).parse(data);
    },
  });
}

export const customerCareStatsSchema = z.object({
  total: z.number(),
  new: z.number(),
  inProgress: z.number(),
  done: z.number(),
  overdue: z.number(),
});

export function useCustomerCareStats() {
  return useQuery({
    queryKey: [...KEY, 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customer-cares/stats');
      return customerCareStatsSchema.parse(data);
    },
  });
}

const customerOptionSchema = z.object({ id: z.string().uuid(), fullName: z.string() });
const pagedCustomers = pagedSchema(customerOptionSchema);

export function useCustomerOptions() {
  return useQuery({
    queryKey: ['customers', 'options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customers', { params: { page: 1, size: 500 } });
      return pagedCustomers.parse(data).items;
    },
  });
}

const userOptionSchema = z.object({ id: z.string().uuid(), fullName: z.string() });

export function useUserOptions() {
  return useQuery({
    queryKey: ['users', 'options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userOptionSchema).parse(data);
    },
  });
}
