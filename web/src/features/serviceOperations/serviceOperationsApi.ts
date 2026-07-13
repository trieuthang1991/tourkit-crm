import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';

const KEY = ['service-operations'];

export const serviceOperationSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  providerName: z.string().nullable(),
  description: z.string(),
  usageDate: z.string().nullable(),
  totalAmount: z.number(),
  paidAmount: z.number(),
  remainingAmount: z.number(),
  paymentStatus: z.number(),
});
export type ServiceOperation = z.infer<typeof serviceOperationSchema>;

export const serviceOperationStatsSchema = z.object({
  total: z.number(),
  unpaid: z.number(),
  partial: z.number(),
  done: z.number(),
  totalCost: z.number(),
  totalPaid: z.number(),
  totalRemaining: z.number(),
});

/// Trạng thái chi: 0 Chờ chi, 1 Chưa chi hết, 2 Thành công.
export const PAYMENT_STATUS: Record<number, string> = { 0: 'Chờ chi', 1: 'Chưa chi hết', 2: 'Thành công' };

export type ServiceOperationFilter = { q?: string; providerId?: string; paymentStatus?: number };

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useServiceOperations(page: number, size: number, filter: ServiceOperationFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/service-operations', { params: cleanParams({ page, size, ...filter }) });
      return pagedSchema(serviceOperationSchema).parse(data);
    },
  });
}

export function useServiceOperationStats(filter: ServiceOperationFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'stats', filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/service-operations/stats', { params: cleanParams({ ...filter }) });
      return serviceOperationStatsSchema.parse(data);
    },
  });
}

export function usePayServiceOperation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, paidAmount }: { id: string; paidAmount: number }) => {
      await httpClient.post(`/api/v1/service-operations/${id}/pay`, { paidAmount });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

const providerOptionSchema = z.object({ id: z.string().uuid(), name: z.string() });
export function useProviderOptions() {
  return useQuery({
    queryKey: ['providers', 'options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/providers', { params: { page: 1, size: 500 } });
      return pagedSchema(providerOptionSchema).parse(data).items;
    },
  });
}
