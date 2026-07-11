import { z } from 'zod';

export const surchargeSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  calcType: z.number(),
  defaultValue: z.number(),
  sortOrder: z.number(),
  status: z.number(),
});
export type Surcharge = z.infer<typeof surchargeSchema>;

export const surchargeCreateSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  calcType: z.number().int().min(0).max(1),
  defaultValue: z.number().min(0),
  sortOrder: z.number(),
});
export type SurchargeCreateForm = z.infer<typeof surchargeCreateSchema>;

export const CALC_TYPE_OPTIONS = [
  { value: 0, label: 'Số tiền cố định' },
  { value: 1, label: '% trên giá gốc' },
];
