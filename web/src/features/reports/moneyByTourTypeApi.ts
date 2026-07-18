import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const moneyByTourTypeRowSchema = z.object({
  bookingType: z.number(),
  tourTypeName: z.string(),
  orderCount: z.number(),
  grossRevenue: z.number(),
  refund: z.number(),
  netRevenue: z.number(),
  cost: z.number(),
  profit: z.number(),
});
export type MoneyByTourTypeRow = z.infer<typeof moneyByTourTypeRowSchema>;

export function useMoneyByTourType() {
  return useQuery({
    queryKey: ['reports', 'money-by-tour-type'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/reports/money-by-tour-type');
      return z.array(moneyByTourTypeRowSchema).parse(data);
    },
  });
}
