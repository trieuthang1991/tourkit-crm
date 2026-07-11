import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const tourTransferSchema = z.object({
  id: z.string().uuid(),
  orderId: z.string().uuid(),
  fromDepartureId: z.string().uuid(),
  toDepartureId: z.string().uuid(),
  reason: z.string().nullable(),
  transferredAt: z.string(),
});
export type TourTransfer = z.infer<typeof tourTransferSchema>;

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
    mutationFn: async ({ toDepartureId, reason }: { toDepartureId: string; reason: string | null }) => {
      const { data } = await httpClient.post<unknown>(`/api/v1/orders/${orderId}/transfers`, { toDepartureId, reason });
      return tourTransferSchema.parse(data);
    },
    // Chuyển chuyến đổi TourDepartureId của đơn → làm mới danh sách đơn + lịch sử chuyển.
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: key(orderId) });
      qc.invalidateQueries({ queryKey: ['orders'] });
    },
  });
}
