import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { QueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { balanceSchema, receiptSchema } from './receiptTypes';
import type { Balance, CreateReceiptForm, Receipt } from './receiptTypes';

const receiptsKey = (orderId: string) => ['orders', orderId, 'receipts'] as const;
const balanceKey = (orderId: string) => ['orders', orderId, 'balance'] as const;

export function useReceipts(orderId: string) {
  return useQuery({
    queryKey: receiptsKey(orderId),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/orders/${orderId}/receipts`);
      return z.array(receiptSchema).parse(data);
    },
    enabled: !!orderId,
  });
}

export function useBalance(orderId: string) {
  return useQuery({
    queryKey: balanceKey(orderId),
    queryFn: async (): Promise<Balance> => {
      const { data } = await httpClient.get<unknown>(`/api/v1/orders/${orderId}/balance`);
      return balanceSchema.parse(data);
    },
    enabled: !!orderId,
  });
}

function invalidateReceiptQueries(qc: QueryClient, orderId: string) {
  qc.invalidateQueries({ queryKey: receiptsKey(orderId) });
  qc.invalidateQueries({ queryKey: balanceKey(orderId) });
}

export function useCreateReceipt(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateReceiptForm): Promise<Receipt> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/orders/${orderId}/receipts`, body);
      return receiptSchema.parse(data);
    },
    onSuccess: () => invalidateReceiptQueries(qc, orderId),
  });
}

export function useApproveReceipt(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (receiptId: string): Promise<Receipt> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/receipts/${receiptId}/approve`);
      return receiptSchema.parse(data);
    },
    onSuccess: () => invalidateReceiptQueries(qc, orderId),
  });
}

export function useRejectReceipt(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (receiptId: string): Promise<Receipt> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/receipts/${receiptId}/reject`);
      return receiptSchema.parse(data);
    },
    onSuccess: () => invalidateReceiptQueries(qc, orderId),
  });
}
