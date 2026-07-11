import { z } from 'zod';

export const customerCommissionRuleSchema = z.object({
  id: z.string().uuid(),
  customerType: z.number(),
  percentage: z.number(),
  status: z.number(),
});
export type CustomerCommissionRule = z.infer<typeof customerCommissionRuleSchema>;

export const customerCommissionRuleFormSchema = z.object({
  customerType: z.number(),
  percentage: z.number(),
  status: z.number(),
});
export type CustomerCommissionRuleForm = z.infer<typeof customerCommissionRuleFormSchema>;
