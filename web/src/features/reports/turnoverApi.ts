import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const turnoverRowSchema = z.object({
  orderId: z.string().uuid(),
  orderCode: z.string(),
  revenue: z.number(),
  cost: z.number(),
  profit: z.number(),
});
export type TurnoverRow = z.infer<typeof turnoverRowSchema>;

const turnoverKey = ['reports', 'turnover'] as const;

export function useTurnover() {
  return useQuery({
    queryKey: turnoverKey,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/reports/turnover');
      return z.array(turnoverRowSchema).parse(data);
    },
  });
}
