import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const commissionByMilestoneRowSchema = z.object({
  userId: z.string().uuid(),
  turnover: z.number(),
  cost: z.number(),
  profit: z.number(),
  commissionRate: z.number(),
  commissionByProfit: z.number(),
  commissionByRevenue: z.number(),
  campaignName: z.string().nullable(),
});
export type CommissionByMilestoneRow = z.infer<typeof commissionByMilestoneRowSchema>;

export type MilestoneRange = { from?: string; to?: string };

const key = (range: MilestoneRange) => ['reports', 'commission-by-milestone', range.from ?? '', range.to ?? ''] as const;

export function useCommissionByMilestone(range: MilestoneRange) {
  return useQuery({
    queryKey: key(range),
    queryFn: async () => {
      const params = new URLSearchParams();
      if (range.from) params.set('from', range.from);
      if (range.to) params.set('to', range.to);
      const qs = params.toString();
      const { data } = await httpClient.get<unknown>(`/api/v1/reports/commission-by-milestone${qs ? `?${qs}` : ''}`);
      return z.array(commissionByMilestoneRowSchema).parse(data);
    },
  });
}
