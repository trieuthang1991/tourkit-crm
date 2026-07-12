import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { providerServiceSchema } from '../services/providerServiceTypes';
import { paymentAccountSchema } from '../paymentAccounts/types';
import { quoteSchema, quoteSummarySchema } from './types';
import type { QuoteForm } from './types';

const KEY = ['quotes'];

/// Tài khoản nhận tiền mặc định — in lên bản báo giá để khách biết chuyển khoản vào đâu.
export function useDefaultPaymentAccount() {
  return useQuery({
    queryKey: ['payment-accounts', 'default'] as const,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/payment-accounts');
      const accounts = z.array(paymentAccountSchema).parse(data);
      return accounts.find((a) => a.isDefault) ?? accounts[0] ?? null;
    },
  });
}

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

export type QuoteFilter = { q?: string; status?: number; validFrom?: string; validTo?: string; converted?: boolean };

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useQuotes(page: number, size: number, filter: QuoteFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/quotes', { params: cleanParams({ page, size, ...filter }) });
      return pagedSchema(quoteSummarySchema).parse(data);
    },
  });
}

export const quoteStatsSchema = z.object({
  total: z.number(),
  draft: z.number(),
  sent: z.number(),
  accepted: z.number(),
  rejected: z.number(),
  totalAmount: z.number(),
  totalProfit: z.number(),
});

export function useQuoteStats() {
  return useQuery({
    queryKey: [...KEY, 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/quotes/stats');
      return quoteStatsSchema.parse(data);
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
