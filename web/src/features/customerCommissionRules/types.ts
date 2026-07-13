import { z } from 'zod';

export const customerCommissionRuleSchema = z.object({
  id: z.string().uuid(),
  customerType: z.number(),
  percentage: z.number(),
  status: z.number(),
  customerTypeName: z.string().nullable().optional(),
});
export type CustomerCommissionRule = z.infer<typeof customerCommissionRuleSchema>;

/// Trạng thái quy tắc (legacy Status): 1 áp dụng, 0 tạm ngừng.
export const CUSTOMER_COMMISSION_STATUS: Record<number, string> = { 1: 'Áp dụng', 0: 'Tạm ngừng' };

export const customerCommissionRuleFormSchema = z.object({
  customerType: z.number(),
  percentage: z.number(),
  status: z.number(),
});
export type CustomerCommissionRuleForm = z.infer<typeof customerCommissionRuleFormSchema>;
