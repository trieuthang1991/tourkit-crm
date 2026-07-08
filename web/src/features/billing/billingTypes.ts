import { z } from 'zod';

export const planSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  name: z.string(),
  maxUsers: z.number(),
  maxTours: z.number(),
  priceMonthly: z.number(),
});
export type Plan = z.infer<typeof planSchema>;

export const subscriptionSchema = z.object({
  id: z.string().uuid(),
  planId: z.string().uuid(),
  planCode: z.string(),
  status: z.number(),
  startedAt: z.string(),
  expiresAt: z.string().nullable(),
});
export type Subscription = z.infer<typeof subscriptionSchema>;

export const SUBSCRIPTION_STATUS: Record<number, string> = {
  1: 'Đang hoạt động',
  2: 'Hết hạn',
  3: 'Đã huỷ',
};
