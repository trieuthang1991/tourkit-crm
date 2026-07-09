import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const cashFlowRowSchema = z.object({
  paymentMethod: z.string(),
  inflow: z.number(),
  outflow: z.number(),
  net: z.number(),
});
export type CashFlowRow = z.infer<typeof cashFlowRowSchema>;

const cashFlowKey = ['reports', 'cash-flow'] as const;

export function useCashFlow() {
  return useQuery({
    queryKey: cashFlowKey,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/reports/cash-flow');
      return z.array(cashFlowRowSchema).parse(data);
    },
  });
}
