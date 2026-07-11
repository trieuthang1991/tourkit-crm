import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const turnoverByDepartmentRowSchema = z.object({
  departmentId: z.string().nullable(),
  departmentName: z.string(),
  orderCount: z.number(),
  turnover: z.number(),
  cost: z.number(),
  profit: z.number(),
});
export type TurnoverByDepartmentRow = z.infer<typeof turnoverByDepartmentRowSchema>;

const key = ['reports', 'turnover-by-department'] as const;

export function useTurnoverByDepartment() {
  return useQuery({
    queryKey: key,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/reports/turnover-by-department');
      return z.array(turnoverByDepartmentRowSchema).parse(data);
    },
  });
}
