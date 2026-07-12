import { z } from 'zod';

export const leadSchema = z.object({
  id: z.string().uuid(),
  fullName: z.string(),
  phone: z.string().nullable(),
  email: z.string().nullable(),
  source: z.string().nullable(),
  status: z.number(),
  assignedToUserId: z.string().uuid().nullable(),
  convertedCustomerId: z.string().uuid().nullable(),
  branchId: z.string().uuid().nullable(),
});
export type Lead = z.infer<typeof leadSchema>;

const leadBaseFields = {
  fullName: z.string().min(1, 'Bắt buộc'),
  phone: z.string().nullable().transform((v) => (v ? v : null)),
  email: z.string().nullable().transform((v) => (v ? v : null)),
  source: z.string().nullable().transform((v) => (v ? v : null)),
  assignedToUserId: z.string().nullable().transform((v) => (v ? v : null)),
  branchId: z.string().nullable().transform((v) => (v ? v : null)),
};

export const leadCreateSchema = z.object(leadBaseFields);
export type LeadCreateForm = z.infer<typeof leadCreateSchema>;

export const leadUpdateSchema = z.object({
  ...leadBaseFields,
  status: z.number(),
});
export type LeadUpdateForm = z.infer<typeof leadUpdateSchema>;

// Unified shape used as the ResourcePage/makeCrud TForm generic — status is optional so both
// leadCreateSchema (no status) and leadUpdateSchema (status required) outputs satisfy it.
export type LeadForm = LeadCreateForm & { status?: number };

export const LEAD_STATUS: Record<number, string> = {
  1: 'Mới',
  2: 'Đã liên hệ',
  3: 'Tiềm năng',
  4: 'Chốt',
  5: 'Mất',
};
