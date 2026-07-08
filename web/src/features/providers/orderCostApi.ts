import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const orderCostSchema = z.object({
  id: z.string().uuid(),
  orderId: z.string().uuid(),
  providerId: z.string().uuid(),
  serviceName: z.string().nullable(),
  dayIndex: z.number(),
  expectedAmount: z.number(),
  actualAmount: z.number(),
  deposit: z.number(),
  surcharge: z.number(),
  vat: z.number(),
  status: z.number(),
});
export type OrderCost = z.infer<typeof orderCostSchema>;

export type CreateOrderCostForm = {
  providerId: string;
  serviceName: string | null;
  dayIndex: number;
  expectedAmount: number;
  actualAmount: number;
  deposit: number;
  surcharge: number;
  vat: number;
  status: number;
};

const costsKey = (orderId: string) => ['orders', orderId, 'costs'] as const;

export function useOrderCosts(orderId: string) {
  return useQuery({
    queryKey: costsKey(orderId),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/orders/${orderId}/costs`);
      return z.array(orderCostSchema).parse(data);
    },
    enabled: !!orderId,
  });
}

export function useCreateOrderCost(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateOrderCostForm): Promise<OrderCost> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/orders/${orderId}/costs`, body);
      return orderCostSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: costsKey(orderId) }),
  });
}
