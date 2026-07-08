import { z } from 'zod';

export const approvalStepSchema = z.object({
  stepOrder: z.number(),
  userId: z.string().uuid(),
  status: z.number(),
  actedAt: z.string().nullable(),
  note: z.string().nullable(),
});
export type ApprovalStep = z.infer<typeof approvalStepSchema>;

export const approvalSchema = z.object({
  id: z.string().uuid(),
  receiptVoucherId: z.string().uuid(),
  method: z.number(),
  currentStepOrder: z.number(),
  status: z.number(),
  steps: z.array(approvalStepSchema),
});
export type Approval = z.infer<typeof approvalSchema>;

export type StartApprovalStepInput = { stepOrder: number; userIds: string[] };
export type StartApprovalForm = { method: number; steps: StartApprovalStepInput[] };

export type ActApprovalForm = { approve: boolean; note: string | null };

export const APPROVAL_METHOD: Record<number, string> = {
  1: 'Một người',
  2: 'Tất cả',
};

export const APPROVAL_STATUS: Record<number, string> = {
  1: 'Đang duyệt',
  2: 'Đã duyệt',
  3: 'Từ chối',
};

export const STEP_STATUS: Record<number, string> = {
  1: 'Chờ',
  2: 'Đã duyệt',
  3: 'Từ chối',
};
