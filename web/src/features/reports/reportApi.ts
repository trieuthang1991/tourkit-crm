import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const orderDebtRowSchema = z.object({
  orderId: z.string().uuid(),
  orderCode: z.string(),
  customerId: z.string().uuid(),
  total: z.number(),
  paid: z.number(),
  outstanding: z.number(),
});
export type OrderDebtRow = z.infer<typeof orderDebtRowSchema>;

const orderDebtKey = ['reports', 'order-debt'] as const;

export function useOrderDebt() {
  return useQuery({
    queryKey: orderDebtKey,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/reports/order-debt');
      return z.array(orderDebtRowSchema).parse(data);
    },
  });
}
