import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { seatSchema } from './seatTypes';
import type { Seat } from './seatTypes';

export const bookingLineSchema = z.object({
  id: z.string().uuid(),
  quantity: z.number(),
  amountChildren: z.number(),
  amountChildrenSmall: z.number(),
  quantityBaby: z.number(),
  priceAdult: z.number(),
  priceChild: z.number(),
  priceChildSmall: z.number(),
  priceBaby: z.number(),
  upfrontAmount: z.number(),
  reservationCode: z.string().nullable(),
  isMainContact: z.boolean(),
});
export type BookingLine = z.infer<typeof bookingLineSchema>;

const linesKey = (orderId: string) => ['orders', orderId, 'lines'] as const;

export function useOrderLines(orderId: string) {
  return useQuery({
    queryKey: linesKey(orderId),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/orders/${orderId}/lines`);
      return z.array(bookingLineSchema).parse(data);
    },
    enabled: !!orderId,
  });
}

// BookingLineResponse.id CHÍNH LÀ TourCustomer/seat id (cùng một entity) — dùng trực tiếp cho
// các action /api/v1/tour-customers/{seatId}/... bên dưới.
function seatActionPath(seatId: string, action: string) {
  return `/api/v1/tour-customers/${seatId}/${action}`;
}

export function useConfirmSeat(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (seatId: string): Promise<Seat> => {
      const { data } = await httpClient.post<unknown>(seatActionPath(seatId, 'confirm-seat'));
      return seatSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: linesKey(orderId) }),
  });
}

export function useDepositSeat(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ seatId, amount }: { seatId: string; amount: number }): Promise<Seat> => {
      const { data } = await httpClient.post<unknown>(seatActionPath(seatId, 'deposit'), { amount });
      return seatSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: linesKey(orderId) }),
  });
}

export function useCancelSeat(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({
      seatId,
      note,
      refundAmount,
    }: {
      seatId: string;
      note: string | null;
      refundAmount: number;
    }): Promise<Seat> => {
      const { data } = await httpClient.post<unknown>(seatActionPath(seatId, 'cancel'), { note, refundAmount });
      return seatSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: linesKey(orderId) }),
  });
}
