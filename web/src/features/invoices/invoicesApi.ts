import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { invoiceSchema, invoiceSummarySchema } from './types';
import type { InvoiceForm } from './types';

const KEY = ['invoices'];

export type InvoiceFilter = { q?: string; status?: number; dateFrom?: string; dateTo?: string };

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useInvoices(page: number, size: number, filter: InvoiceFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/invoices', { params: cleanParams({ page, size, ...filter }) });
      return pagedSchema(invoiceSummarySchema).parse(data);
    },
  });
}

export const invoiceStatsSchema = z.object({
  total: z.number(),
  draft: z.number(),
  issued: z.number(),
  cancelled: z.number(),
  totalAmount: z.number(),
  totalVat: z.number(),
});

export function useInvoiceStats() {
  return useQuery({
    queryKey: [...KEY, 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/invoices/stats');
      return invoiceStatsSchema.parse(data);
    },
  });
}

export function useInvoice(id: string) {
  return useQuery({
    queryKey: [...KEY, id],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/invoices/${id}`);
      return invoiceSchema.parse(data);
    },
    enabled: !!id,
  });
}

export function useCreateInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: InvoiceForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/invoices', body);
      return invoiceSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useUpdateInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: InvoiceForm }) => {
      await httpClient.put(`/api/v1/invoices/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/invoices/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
