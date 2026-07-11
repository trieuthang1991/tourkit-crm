import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const guideTxType = { revenue: 0, expense: 1 } as const;

const guideTransactionSchema = z.object({
  id: z.string().uuid(),
  tourGuideAssignmentId: z.string().uuid(),
  type: z.number(),
  amount: z.number(),
  description: z.string(),
  occurredAt: z.string(),
});
export type GuideTransaction = z.infer<typeof guideTransactionSchema>;

export const guideSettlementSchema = z.object({
  totalRevenue: z.number(),
  totalExpense: z.number(),
  net: z.number(),
  items: z.array(guideTransactionSchema),
});
export type GuideSettlement = z.infer<typeof guideSettlementSchema>;

export type CreateGuideTransactionForm = { type: number; amount: number; description: string };

const key = (assignmentId: string) => ['guide-assignments', assignmentId, 'transactions'] as const;

export function useGuideSettlement(assignmentId: string, enabled: boolean) {
  return useQuery({
    queryKey: key(assignmentId),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/guide-assignments/${assignmentId}/transactions`);
      return guideSettlementSchema.parse(data);
    },
    enabled,
  });
}

export function useCreateGuideTransaction(assignmentId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateGuideTransactionForm) => {
      await httpClient.post(`/api/v1/guide-assignments/${assignmentId}/transactions`, { ...body, occurredAt: null });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: key(assignmentId) }),
  });
}

export function useDeleteGuideTransaction(assignmentId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/guide-assignments/${assignmentId}/transactions/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: key(assignmentId) }),
  });
}
