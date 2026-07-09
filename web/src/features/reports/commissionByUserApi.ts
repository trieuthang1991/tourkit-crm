import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const commissionByUserRowSchema = z.object({
  userId: z.string().uuid(),
  turnover: z.number(),
  cost: z.number(),
  profit: z.number(),
  commissionRate: z.number(),
  commissionAmount: z.number(),
});
export type CommissionByUserRow = z.infer<typeof commissionByUserRowSchema>;

const commissionByUserKey = ['reports', 'commission-by-user'] as const;

export function useCommissionByUser() {
  return useQuery({
    queryKey: commissionByUserKey,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/reports/commission-by-user');
      return z.array(commissionByUserRowSchema).parse(data);
    },
  });
}
