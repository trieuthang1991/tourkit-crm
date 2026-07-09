import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const dashboardSummarySchema = z.object({
  orderCount: z.number(),
  totalRevenue: z.number(),
  totalReceived: z.number(),
  receivableOutstanding: z.number(),
  totalCost: z.number(),
  totalPaid: z.number(),
  payableOutstanding: z.number(),
  grossProfit: z.number(),
});
export type DashboardSummary = z.infer<typeof dashboardSummarySchema>;

const dashboardKey = ['reports', 'dashboard'] as const;

export function useDashboard() {
  return useQuery({
    queryKey: dashboardKey,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/reports/dashboard');
      return dashboardSummarySchema.parse(data);
    },
  });
}
