import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { invoiceSchema, invoiceSummarySchema } from './types';
import type { InvoiceForm } from './types';

const KEY = ['invoices'];

export function useInvoices(page: number, size: number) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/invoices', { params: { page, size } });
      return pagedSchema(invoiceSummarySchema).parse(data);
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
