import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

/* =========================================================================
   Lịch thanh toán NCC (payment terms) + cảnh báo đến hạn / quá hạn.
   Endpoint đã live: /service-bookings/{bookingId}/payment-terms (CRUD)
   và /service-operations/due-alerts?withinDays=N.
   ========================================================================= */

/// Trạng thái đợt chi: 0 Chờ chi, 1 Đã chi.
export const PAYMENT_TERM_STATUS: Record<number, string> = { 0: 'Chờ chi', 1: 'Đã chi' };

export const paymentTermSchema = z.object({
  id: z.string(),
  serviceBookingId: z.string(),
  orderCostId: z.string().nullable().optional(),
  amount: z.number(),
  dueDate: z.string(),
  note: z.string().nullable().optional(),
  status: z.number(),
  paidAmount: z.number(),
  paymentVoucherId: z.string().nullable().optional(),
});
export type PaymentTerm = z.infer<typeof paymentTermSchema>;

export type PaymentTermForm = {
  amount: number;
  dueDate: string;
  note?: string | null;
  status: number;
  paidAmount: number;
  orderCostId?: string | null;
};

export const dueAlertSchema = z.object({
  id: z.string(),
  serviceBookingId: z.string(),
  serviceCode: z.string(),
  providerName: z.string().nullable().optional(),
  amount: z.number(),
  paidAmount: z.number(),
  remainingAmount: z.number(),
  dueDate: z.string(),
  daysUntilDue: z.number(),
  isOverdue: z.boolean(),
  note: z.string().nullable().optional(),
});
export type DueAlert = z.infer<typeof dueAlertSchema>;

const termsKey = (bookingId: string) => ['service-bookings', bookingId, 'payment-terms'];

/** Khi lịch TT đổi thì list điều hành + cảnh báo đến hạn đều phải làm mới. */
function invalidateRelated(qc: ReturnType<typeof useQueryClient>, bookingId: string) {
  qc.invalidateQueries({ queryKey: termsKey(bookingId) });
  qc.invalidateQueries({ queryKey: ['service-operations'] });
}

export function usePaymentTerms(bookingId: string | undefined) {
  return useQuery({
    queryKey: termsKey(bookingId ?? ''),
    enabled: !!bookingId,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/service-bookings/${bookingId}/payment-terms`);
      return z.array(paymentTermSchema).parse(data);
    },
  });
}

export function useCreatePaymentTerm(bookingId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: PaymentTermForm) => {
      await httpClient.post(`/api/v1/service-bookings/${bookingId}/payment-terms`, body);
    },
    onSuccess: () => invalidateRelated(qc, bookingId),
  });
}

export function useUpdatePaymentTerm(bookingId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: PaymentTermForm }) => {
      await httpClient.put(`/api/v1/service-bookings/${bookingId}/payment-terms/${id}`, body);
    },
    onSuccess: () => invalidateRelated(qc, bookingId),
  });
}

export function useDeletePaymentTerm(bookingId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/service-bookings/${bookingId}/payment-terms/${id}`);
    },
    onSuccess: () => invalidateRelated(qc, bookingId),
  });
}

export function useDueAlerts(withinDays = 7) {
  return useQuery({
    queryKey: ['service-operations', 'due-alerts', withinDays],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/service-operations/due-alerts', { params: { withinDays } });
      return z.array(dueAlertSchema).parse(data);
    },
  });
}
