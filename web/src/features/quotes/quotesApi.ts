import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { quoteSchema, quoteSummarySchema } from './types';
import type { QuoteForm } from './types';

const KEY = ['quotes'];

export function useQuotes(page: number, size: number) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/quotes', { params: { page, size } });
      return pagedSchema(quoteSummarySchema).parse(data);
    },
  });
}

export function useQuote(id: string) {
  return useQuery({
    queryKey: [...KEY, id],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/quotes/${id}`);
      return quoteSchema.parse(data);
    },
    enabled: !!id,
  });
}

export function useCreateQuote() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: QuoteForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/quotes', body);
      return quoteSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useUpdateQuote() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: QuoteForm }) => {
      await httpClient.put(`/api/v1/quotes/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteQuote() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/quotes/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
