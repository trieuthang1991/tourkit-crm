import { z } from 'zod';

export const customerTypeSchema = z.object({
  id: z.string().uuid(),
  code: z.number(),
  name: z.string(),
  sortOrder: z.number(),
  status: z.number(),
});
export type CustomerType = z.infer<typeof customerTypeSchema>;

export const customerTypeCreateSchema = z.object({
  code: z.number().int().min(1, 'Mã > 0'),
  name: z.string().min(1, 'Bắt buộc'),
  sortOrder: z.number(),
});
export type CustomerTypeCreateForm = z.infer<typeof customerTypeCreateSchema>;
