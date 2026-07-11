import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const tourTransferSchema = z.object({
  id: z.string().uuid(),
  orderId: z.string().uuid(),
  fromDepartureId: z.string().uuid(),
  toDepartureId: z.string().uuid(),
  reason: z.string().nullable(),
  reasonId: z.string().nullable(),
  reasonName: z.string().nullable(),
  transferredAt: z.string(),
});
export type TourTransfer = z.infer<typeof tourTransferSchema>;

export const transferReasonSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  sortOrder: z.number(),
  status: z.number(),
});
export type TransferReason = z.infer<typeof transferReasonSchema>;

export function useTransferReasons() {
  return useQuery({
    queryKey: ['transfer-reasons'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/transfer-reasons');
      return z.array(transferReasonSchema).parse(data);
    },
  });
}

const key = (orderId: string) => ['orders', orderId, 'transfers'] as const;

export function useTransfers(orderId: string) {
  return useQuery({
    queryKey: key(orderId),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/orders/${orderId}/transfers`);
      return z.array(tourTransferSchema).parse(data);
    },
    enabled: !!orderId,
  });
}

export function useTransferOrder(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({
      toDepartureId,
      reason,
      reasonId,
    }: {
      toDepartureId: string;
      reason: string | null;
      reasonId: string | null;
    }) => {
      const { data } = await httpClient.post<unknown>(`/api/v1/orders/${orderId}/transfers`, {
        toDepartureId,
        reason,
        reasonId,
      });
      return tourTransferSchema.parse(data);
    },
    // Chuyển chuyến đổi TourDepartureId của đơn → làm mới danh sách đơn + lịch sử chuyển.
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: key(orderId) });
      qc.invalidateQueries({ queryKey: ['orders'] });
    },
  });
}
