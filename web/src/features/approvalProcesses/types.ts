import { z } from 'zod';

export const APPROVAL_METHOD_OPTIONS = [
  { value: 1, label: 'Một người bước đó duyệt là qua' },
  { value: 2, label: 'Tất cả người bước đó phải duyệt' },
];
export const approvalMethodLabel = (v: number) =>
  APPROVAL_METHOD_OPTIONS.find((o) => o.value === v)?.label ?? '';

export const approvalProcessSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  method: z.number(),
  status: z.number(),
  stepCount: z.number(),
});
export type ApprovalProcess = z.infer<typeof approvalProcessSchema>;

export const approvalStepSchema = z.object({
  id: z.string().uuid(),
  stepOrder: z.number(),
  positionId: z.string().uuid(),
  positionName: z.string().nullable(),
  userIds: z.array(z.string().uuid()),
  userNames: z.array(z.string()),
});
export type ApprovalStep = z.infer<typeof approvalStepSchema>;

export const approvalProcessDetailSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  method: z.number(),
  status: z.number(),
  steps: z.array(approvalStepSchema),
});
export type ApprovalProcessDetail = z.infer<typeof approvalProcessDetailSchema>;
