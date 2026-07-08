import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const profitSchema = z.object({
  revenue: z.number(),
  cost: z.number(),
  profit: z.number(),
});
export type Profit = z.infer<typeof profitSchema>;

export const profitShareSchema = z.object({
  id: z.string().uuid(),
  orderId: z.string().uuid(),
  userId: z.string().uuid(),
  percentage: z.number(),
  amount: z.number(),
  profitBase: z.number(),
});
export type ProfitShare = z.infer<typeof profitShareSchema>;

export type CreateProfitShareForm = {
  userId: string;
  percentage: number;
};

const profitKey = (orderId: string) => ['orders', orderId, 'profit'] as const;
const profitSharesKey = (orderId: string) => ['orders', orderId, 'profit-shares'] as const;

export function useOrderProfit(orderId: string) {
  return useQuery({
    queryKey: profitKey(orderId),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/orders/${orderId}/profit`);
      return profitSchema.parse(data);
    },
    enabled: !!orderId,
  });
}

export function useProfitShares(orderId: string) {
  return useQuery({
    queryKey: profitSharesKey(orderId),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/orders/${orderId}/profit-shares`);
      return z.array(profitShareSchema).parse(data);
    },
    enabled: !!orderId,
  });
}

export function useCreateProfitShare(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateProfitShareForm): Promise<ProfitShare> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/orders/${orderId}/profit-shares`, body);
      return profitShareSchema.parse(data);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: profitSharesKey(orderId) });
      qc.invalidateQueries({ queryKey: profitKey(orderId) });
    },
  });
}
