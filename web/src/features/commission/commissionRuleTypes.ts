import { z } from 'zod';

export const commissionRuleSchema = z.object({
  id: z.string().uuid(),
  userId: z.string().uuid(),
  percentage: z.number(),
  status: z.number(),
});
export type CommissionRule = z.infer<typeof commissionRuleSchema>;

const commissionRuleCommonFields = {
  percentage: z.number(),
  status: z.number(),
};

export const commissionRuleCreateSchema = z.object({
  userId: z.string().uuid('Bắt buộc'),
  ...commissionRuleCommonFields,
});
export type CommissionRuleCreateForm = z.infer<typeof commissionRuleCreateSchema>;

export const commissionRuleUpdateSchema = z.object(commissionRuleCommonFields);
export type CommissionRuleUpdateForm = z.infer<typeof commissionRuleUpdateSchema>;

// Unified shape used as the ResourcePage/makeCrud TForm generic — userId is optional so both
// commissionRuleCreateSchema (with userId) and commissionRuleUpdateSchema (no userId) outputs
// satisfy it.
export type CommissionRuleForm = CommissionRuleUpdateForm & { userId?: string };
