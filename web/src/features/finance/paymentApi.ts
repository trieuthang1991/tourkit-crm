import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { QueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { paymentSchema } from './paymentTypes';
import type { CreatePaymentForm, Payment } from './paymentTypes';

const paymentsKey = (orderId: string) => ['orders', orderId, 'payments'] as const;
const providerDebtKey = ['reports', 'provider-debt'] as const;

export function usePayments(orderId: string) {
  return useQuery({
    queryKey: paymentsKey(orderId),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/orders/${orderId}/payments`);
      return z.array(paymentSchema).parse(data);
    },
    enabled: !!orderId,
  });
}

function invalidatePaymentQueries(qc: QueryClient, orderId: string) {
  qc.invalidateQueries({ queryKey: paymentsKey(orderId) });
  qc.invalidateQueries({ queryKey: providerDebtKey });
}

export function useCreatePayment(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreatePaymentForm): Promise<Payment> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/orders/${orderId}/payments`, body);
      return paymentSchema.parse(data);
    },
    onSuccess: () => invalidatePaymentQueries(qc, orderId),
  });
}

export function useApprovePayment(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (paymentId: string): Promise<Payment> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/payments/${paymentId}/approve`);
      return paymentSchema.parse(data);
    },
    onSuccess: () => invalidatePaymentQueries(qc, orderId),
  });
}

export function useRejectPayment(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (paymentId: string): Promise<Payment> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/payments/${paymentId}/reject`);
      return paymentSchema.parse(data);
    },
    onSuccess: () => invalidatePaymentQueries(qc, orderId),
  });
}
