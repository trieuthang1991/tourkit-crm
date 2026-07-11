import { z } from 'zod';

export const workTaskSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  description: z.string().nullable(),
  assigneeUserId: z.string().nullable(),
  assigneeName: z.string().nullable(),
  dueDate: z.string().nullable(),
  priority: z.number(),
  status: z.number(),
  relatedOrderId: z.string().nullable(),
});
export type WorkTask = z.infer<typeof workTaskSchema>;

export const workTaskFormSchema = z.object({
  title: z.string().min(1, 'Bắt buộc'),
  description: z.string().nullable().transform((v) => (v ? v : null)),
  assigneeUserId: z.string().nullable(),
  dueDate: z.string().nullable(),
  priority: z.number(),
  status: z.number(),
  relatedOrderId: z.string().nullable(),
});
export type WorkTaskForm = z.infer<typeof workTaskFormSchema>;

export const PRIORITY_OPTIONS = [
  { value: 0, label: 'Thấp' },
  { value: 1, label: 'Bình thường' },
  { value: 2, label: 'Cao' },
];

export const STATUS_OPTIONS = [
  { value: 0, label: 'Cần làm' },
  { value: 1, label: 'Đang làm' },
  { value: 2, label: 'Hoàn thành' },
  { value: 3, label: 'Huỷ' },
];

export const priorityLabel = (v: number) => PRIORITY_OPTIONS.find((o) => o.value === v)?.label ?? '';
export const statusLabel = (v: number) => STATUS_OPTIONS.find((o) => o.value === v)?.label ?? '';
