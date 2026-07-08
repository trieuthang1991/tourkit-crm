import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { planSchema, subscriptionSchema } from './billingTypes';

export type ChangePlanForm = { planCode: string };

const plansKey = ['plans'] as const;
const subscriptionKey = ['subscription'] as const;

export function usePlans() {
  return useQuery({
    queryKey: plansKey,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/plans');
      return z.array(planSchema).parse(data);
    },
  });
}

export function useSubscription() {
  return useQuery({
    queryKey: subscriptionKey,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/subscription');
      return subscriptionSchema.parse(data);
    },
  });
}

export function useChangePlan() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: ChangePlanForm) => {
      // Backend trả về subscription mới hoặc 204 No Content tuỳ trường hợp — không parse response,
      // chỉ invalidate để refetch subscription hiện tại.
      await httpClient.post('/api/v1/subscription/change-plan', body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: subscriptionKey }),
  });
}
