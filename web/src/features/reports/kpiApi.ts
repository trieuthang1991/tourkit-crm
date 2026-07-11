import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const kpiSummarySchema = z.object({
  quoteCount: z.number(),
  quoteAcceptedCount: z.number(),
  quoteConvertedCount: z.number(),
  acceptanceRate: z.number(),
  conversionRate: z.number(),
  orderCount: z.number(),
  totalRevenue: z.number(),
  avgOrderValue: z.number(),
  totalReceived: z.number(),
  collectionRate: z.number(),
});
export type KpiSummary = z.infer<typeof kpiSummarySchema>;

export function useKpiSummary() {
  return useQuery({
    queryKey: ['reports', 'kpi'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/reports/kpi');
      return kpiSummarySchema.parse(data);
    },
  });
}
