import { z } from 'zod';

export const customerCareSchema = z.object({
  id: z.string().uuid(),
  customerId: z.string().uuid(),
  title: z.string(),
  detail: z.string().nullable(),
  remindAt: z.string().nullable(),
  feedback: z.string().nullable(),
  assignedToUserId: z.string().uuid().nullable(),
  status: z.number(),
  customerName: z.string().nullable().optional(),
  assigneeName: z.string().nullable().optional(),
});
export type CustomerCare = z.infer<typeof customerCareSchema>;

/// Trạng thái chăm sóc (legacy): 0 mới, 1 đang xử lý, 2 hoàn thành.
export const CARE_STATUS: Record<number, string> = { 0: 'Mới', 1: 'Đang xử lý', 2: 'Hoàn thành' };

const customerCareCommonFields = {
  title: z.string().min(1, 'Bắt buộc'),
  detail: z.string().nullable().transform((v) => (v ? v : null)),
  remindAt: z.string().nullable(),
  assignedToUserId: z.string().nullable().transform((v) => (v ? v : null)),
  status: z.number(),
};

export const customerCareCreateSchema = z.object({
  customerId: z.string().min(1, 'Bắt buộc'),
  ...customerCareCommonFields,
});
export type CustomerCareCreateForm = z.infer<typeof customerCareCreateSchema>;

export const customerCareUpdateSchema = z.object({
  ...customerCareCommonFields,
  feedback: z.string().nullable().transform((v) => (v ? v : null)),
});
export type CustomerCareUpdateForm = z.infer<typeof customerCareUpdateSchema>;

// Unified shape used as the ResourcePage/makeCrud TForm generic — a superset of create (customerId,
// no feedback) and update (feedback, no customerId) so both schemas' outputs satisfy it.
export type CustomerCareForm = Omit<CustomerCareUpdateForm, 'feedback'> & {
  customerId?: string;
  feedback?: string | null;
};
