import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { providerServiceSchema } from '../services/providerServiceTypes';
import { quoteSchema, quoteSummarySchema } from './types';
import type { QuoteForm } from './types';

const KEY = ['quotes'];

/// Toàn bộ bảng giá NCC (mọi provider) — nguồn giá vốn cho dòng dự trù (spec 2026-07-11).
export function useAllProviderPrices() {
  return useQuery({
    queryKey: ['provider-prices', 'all'] as const,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/provider-services', {
        params: { page: 1, size: 200 },
      });
      return z.object({ items: z.array(providerServiceSchema) }).parse(data).items;
    },
  });
}

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

/// Chuyển báo giá CHẤP NHẬN → đơn + đặt dịch vụ lẻ (legacy DuyetBooking).
/// Ghép chuyến sẵn (tourDepartureId) HOẶC tour lẻ FIT (departureDate → hệ tạo chuyến riêng).
export function useConvertQuote() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({
      id,
      tourDepartureId,
      departureDate,
    }: {
      id: string;
      tourDepartureId: string | null;
      departureDate?: string | null;
    }) => {
      const { data } = await httpClient.post<unknown>(`/api/v1/quotes/${id}/convert`, {
        tourDepartureId,
        departureDate: departureDate ?? null,
      });
      return z
        .object({ orderId: z.string().uuid(), orderCode: z.string(), serviceBookingCount: z.number() })
        .parse(data);
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
